namespace Sitecore.SharedSource.Publishing.Pipelines.Publish
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Jobs;
  using Sitecore.Publishing;
  using Sitecore.Publishing.Pipelines.Publish;
  using Sitecore.Publishing.Pipelines.PublishItem;
  using Sitecore.Security.Accounts;
  using Sitecore.Threading;

  [UsedImplicitly]
  public class ProcessQueue : PublishProcessor
  {
    #region Fields

    private readonly AutoResetEvent alldone = new AutoResetEvent(false);
    private readonly int allowedThreads = Sitecore.Configuration.Settings.GetIntSetting("Publishing.MaxConcurrentThreads", Math.Max(Environment.ProcessorCount, 1));
    private int threadCount;

    #endregion

    #region Public methods

    public override void Process([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      Assert.IsTrue(this.TakeThread(), "Failed to obtain a thread");

      Log.Info(string.Format("Max number of allowed threads for publishing process: {0}", this.allowedThreads), this);
      
      var queue = context.Queue;
      Assert.IsNotNull(queue, "queue");

      foreach (var enumerable in queue)
      {
        if (this.CanTerminate(context))
        {
          this.TerminatePublish(context);
          break;
        }

        if (enumerable == null)
        {
          continue;
        }

        this.ProcessEntries(enumerable, context);
      }

      this.ReleaseThread();
      this.alldone.WaitOne();
      this.UpdateJobStatus(context);
    }

    #endregion

    #region Protected methods

    protected virtual bool CanTerminate([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      if (!this.IsJobExpiry(context.Job))
      {
        return this.IsJobFinished(context.Job);
      }

      return true;
    }

    [CanBeNull]
    protected PublishStatus GetPublishStatus([CanBeNull] Job job)
    {
      if (job == null)
      {
        return null;
      }

      var customData = job.Options.CustomData as PublishStatus;
      if (customData == null)
      {
        return null;
      }

      return customData;
    }

    protected bool IsJobExpiry([CanBeNull] Job job)
    {
      return job != null && job.Status.Expiry < DateTime.Now;
    }

    protected bool IsJobFinished([CanBeNull] Job job)
    {
      var publishStatus = this.GetPublishStatus(job);
      return publishStatus != null && publishStatus.State == JobState.Finished;
    }

    protected virtual void ProcessEntries([NotNull] IEnumerable<PublishingCandidate> entries, [NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(entries, "entries");
      Assert.ArgumentNotNull(context, "context");

      foreach (PublishingCandidate candidate in entries)
      {
        if (this.CanTerminate(context))
        {
          this.TerminatePublish(context);
          break;
        }

        if (candidate == null)
        {
          continue;
        }

        var result = PublishItemPipeline.Run(this.CreateItemContext(candidate, context));
        Assert.IsNotNull(result, "result");

        if (!this.SkipReferrers(result, context))
        {
          var referredItems = result.ReferredItems;
          Assert.IsNotNull(referredItems, "referredItems");

          this.ProcessEntries(referredItems, context);
        }

        if (this.SkipChildren(result, candidate, context))
        {
          break;
        }

        if (this.TakeThread())
        {
          var callContext = new
          {
            Entry = candidate,
            Site = Context.Site,
            User = Context.User
          };

          ManagedThreadPool.QueueUserWorkItem(delegate
          {
            try
            {
              if (callContext.Site != null)
              {
                Context.SetActiveSite(callContext.Site.Name);
              }

              using (new UserSwitcher(callContext.User))
              {
                var childEntries = callContext.Entry.ChildEntries;
                Assert.IsNotNull(childEntries, "childEntries");

                this.ProcessEntries(childEntries, context);
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
          var childEntries = candidate.ChildEntries;
          Assert.IsNotNull(childEntries, "childEntries");

          this.ProcessEntries(childEntries, context);
        }
      }
    }

    protected void SetSkipChildren([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      context.PublishOptions.Deep = false;
    }

    protected virtual bool SkipReferrers([NotNull] PublishItemResult result, [CanBeNull] PublishContext context)
    {
      Assert.ArgumentNotNull(result, "result");

      return result.ReferredItems.Count == 0;
    }

    protected virtual void TerminatePublish([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      Log.Info("Terminating publish: " + (context.Job != null ? context.Job.Handle.ToString() : "<unknown>"), this);

      this.SetSkipChildren(context);
      context.CustomData["IsPublishCanceled"] = true;
    }

    #endregion

    #region Private methods

    [NotNull]
    private PublishItemContext CreateItemContext([NotNull] PublishingCandidate entry, [NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(entry, "entry");
      Assert.ArgumentNotNull(context, "context");

      var context2 = PublishManager.CreatePublishItemContext(entry.ItemId, entry.PublishOptions);
      context2.Job = context.Job;
      context2.User = context.User;
      context2.PublishContext = context;

      return context2;
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

    private bool SkipChildren([NotNull] PublishItemResult result, [NotNull] PublishingCandidate entry, [NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(result, "result");
      Assert.ArgumentNotNull(entry, "entry");
      Assert.ArgumentNotNull(context, "context");

      if (result.ChildAction == PublishChildAction.Skip)
      {
        return true;
      }

      if (result.ChildAction != PublishChildAction.Allow)
      {
        return false;
      }

      if (entry.PublishOptions.Mode != PublishMode.SingleItem && (result.Operation == PublishOperation.Created))
      {
        return false;
      }

      return !entry.PublishOptions.Deep;
    }

    private bool TakeThread()
    {
      lock (this)
      {
        if (this.threadCount >= this.allowedThreads)
        {
          return false;
        }

        this.threadCount++;
        return true;
      }
    }

    private void UpdateJobStatus([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      var job = context.Job;
      if (job == null)
      {
        return;
      }

      job.Status.LogInfo("{0}{1}", Translate.Text("Items created: "), context.Statistics.Created);
      job.Status.LogInfo("{0}{1}", Translate.Text("Items deleted: "), context.Statistics.Deleted);
      job.Status.LogInfo("{0}{1}", Translate.Text("Items updated: "), context.Statistics.Updated);
      job.Status.LogInfo("{0}{1}", Translate.Text("Items skipped: "), context.Statistics.Skipped);
    }

    #endregion
  }
}