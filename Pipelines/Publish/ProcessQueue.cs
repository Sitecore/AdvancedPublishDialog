namespace Sitecore.SharedSource.Publishing.Pipelines.Publish
{
    using Sitecore;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Jobs;
    using Sitecore.Publishing;
    using Sitecore.Publishing.Pipelines.Publish;
    using Sitecore.Publishing.Pipelines.PublishItem;
    using Sitecore.Security.Accounts;
    using Sitecore.Threading;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class ProcessQueue : PublishProcessor
    {
        private readonly AutoResetEvent alldone = new AutoResetEvent(false);
        private readonly int allowedThreads = Settings.GetIntSetting("Publishing.MaxConcurrentThreads", Math.Max(Environment.ProcessorCount, 1));
        private int threadCount;

        protected virtual bool CanTerminate(PublishContext context)
        {
            if (!this.IsJobExpiry(context.Job))
            {
                return this.IsJobFinished(context.Job);
            }
            return true;
        }

        private PublishItemContext CreateItemContext(PublishingCandidate entry, PublishContext context)
        {
            Assert.ArgumentNotNull(entry, "entry");
            PublishItemContext context2 = PublishManager.CreatePublishItemContext(entry.ItemId, entry.PublishOptions);
            context2.Job = context.Job;
            context2.User = context.User;
            context2.PublishContext = context;
            return context2;
        }

        protected PublishStatus GetPublishStatus(Job job)
        {
            if (job != null)
            {
                PublishStatus customData = job.Options.CustomData as PublishStatus;
                if (customData != null)
                {
                    return customData;
                }
            }
            return null;
        }

        protected bool IsJobExpiry(Job job)
        {
            return ((job != null) && (job.Status.Expiry < DateTime.Now));
        }

        protected bool IsJobFinished(Job job)
        {
            PublishStatus publishStatus = this.GetPublishStatus(job);
            return ((publishStatus != null) && (publishStatus.State == JobState.Finished));
        }

        public override void Process(PublishContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.IsTrue(this.TakeThread(), "Failed to obtain a thread");
            Log.Info(string.Format("Max number of allowed threads for publishing process: {0}", this.allowedThreads), this);
            foreach (IEnumerable<PublishingCandidate> enumerable in context.Queue)
            {
                if (this.CanTerminate(context))
                {
                    this.TerminatePublish(context);
                    break;
                }
                this.ProcessEntries(enumerable, context);
            }
            this.ReleaseThread();
            this.alldone.WaitOne();
            this.UpdateJobStatus(context);
        }

        protected virtual void ProcessEntries(IEnumerable<PublishingCandidate> entries, PublishContext context)
        {
            foreach (PublishingCandidate candidate in entries)
            {
                if (this.CanTerminate(context))
                {
                    this.TerminatePublish(context);
                    break;
                }
                PublishItemResult result = PublishItemPipeline.Run(this.CreateItemContext(candidate, context));
                if (!this.SkipReferrers(result, context))
                {
                    this.ProcessEntries(result.ReferredItems, context);
                }
                if (this.SkipChildren(result, candidate, context))
                {
                    break;
                }
                if (this.TakeThread())
                {
                    var callContext = new {
                        Entry = candidate,
                        Site = Context.Site,
                        User = Context.User
                    };
                    ManagedThreadPool.QueueUserWorkItem(delegate (object x) {
                        try
                        {
                            if (callContext.Site != null)
                            {
                                Context.SetActiveSite(callContext.Site.Name);
                            }
                            using (new UserSwitcher(callContext.User))
                            {
                                this.ProcessEntries(callContext.Entry.ChildEntries, context);
                            }
                        }
                        finally
                        {
                            this.ReleaseThread();
                        }
                    });
                }
                else
                {
                    this.ProcessEntries(candidate.ChildEntries, context);
                }
            }
        }

        private void ReleaseThread()
        {
            lock (this)
            {
                this.threadCount--;
                if (this.threadCount == 0)
                {
                    this.alldone.Set();
                }
            }
        }

        protected void SetSkipChildren(PublishContext context)
        {
            context.PublishOptions.Deep = false;
        }

        private bool SkipChildren(PublishItemResult result, PublishingCandidate entry, PublishContext context)
        {
            if (result.ChildAction == PublishChildAction.Skip)
            {
                return true;
            }
            if (result.ChildAction != PublishChildAction.Allow)
            {
                return false;
            }
            if ((entry.PublishOptions.Mode != PublishMode.SingleItem) && (result.Operation == PublishOperation.Created))
            {
                return false;
            }
            return !entry.PublishOptions.Deep;
        }

        protected virtual bool SkipReferrers(PublishItemResult result, PublishContext context)
        {
            return (result.ReferredItems.Count == 0);
        }

        private bool TakeThread()
        {
            lock (this)
            {
                if (this.threadCount < this.allowedThreads)
                {
                    this.threadCount++;
                    return true;
                }
            }
            return false;
        }

        protected virtual void TerminatePublish(PublishContext context)
        {
            this.SetSkipChildren(context);
            context.CustomData["IsPublishCanceled"] = true;
        }

        private void UpdateJobStatus(PublishContext context)
        {
            Job job = context.Job;
            if (job != null)
            {
                job.Status.LogInfo("{0}{1}", new object[] { Translate.Text("Items created: "), context.Statistics.Created });
                job.Status.LogInfo("{0}{1}", new object[] { Translate.Text("Items deleted: "), context.Statistics.Deleted });
                job.Status.LogInfo("{0}{1}", new object[] { Translate.Text("Items updated: "), context.Statistics.Updated });
                job.Status.LogInfo("{0}{1}", new object[] { Translate.Text("Items skipped: "), context.Statistics.Skipped });
            }
        }
    }
}

