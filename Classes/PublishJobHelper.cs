namespace Sitecore.SharedSource.Publishing.Classes
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Sitecore.Collections;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Jobs;
  using Sitecore.Publishing;
  using Sitecore.Web.UI.HtmlControls;

  public static class PublishJobHelper
  {
    #region Public methods

    /// <summary>
    ///   Cancels a job.
    /// </summary>
    /// <param name="cancelOwner">An object that calls the method.</param>
    /// <param name="job">Job instance.</param>
    public static void CancelJob([NotNull] object cancelOwner, [CanBeNull] Job job)
    {
      Assert.ArgumentNotNull(cancelOwner, "cancelOwner");

      if (job != null)
      {
        // Expire the job now
        job.Status.Expiry = DateTime.Now;
        var status = job.Options.CustomData as PublishStatus;
        if (status != null)
        {
          status.Messages.Add(string.Format(Translate.Text("Publishing job was forced to finish by \"{0}\" user"), Context.GetUserName()));
        }

        if (job.Status.State == JobState.Queued)
        {
          FinishJob(job);
        }
        else
        {
          if (status != null)
          {
            // Set job state to Finish to force it quit.
            status.SetState(JobState.Finished);
          }
        }

        Log.Audit(cancelOwner, "Publish cancel: Publishing job \"{1}/{2}\" was forced to finish by \"{0}\" user", Context.GetUserName(), job.Name, job.Handle.ToString());
      }
      else
      {
        Log.SingleWarn("Publish cancel: Failed to cancel a publishing job. The job was not found.", cancelOwner);
      }
    }

    /// <summary>
    ///   Cancels a job.
    /// </summary>
    /// <param name="cancelOwner">An object that calls the method.</param>
    /// <param name="jobHandle">A job handle.</param>
    public static void CancelJob([NotNull] object cancelOwner, [NotNull] string jobHandle)
    {
      Assert.ArgumentNotNull(cancelOwner, "cancelOwner");
      Assert.ArgumentNotNull(jobHandle, "jobHandle");

      var job = JobManager.GetJob(Handle.Parse(jobHandle));
      CancelJob(cancelOwner, job);
    }

    /// <summary>
    ///   Returns publish related jobs.
    /// </summary>
    /// <returns></returns>
    [NotNull]
    public static IEnumerable<PublishJobEntry> GetJobs()
    {
      var jobs = JobManager.GetJobs();
      Assert.IsNotNull(jobs, "jobs");

      var publishJobs = jobs
        .Where(job => job.Category.StartsWith("publish", StringComparison.InvariantCultureIgnoreCase))
        .Select(job => new PublishJobEntry(job.Handle, job.Name, job.Category, job.Status, job.Options.ContextUser));

      return new List<PublishJobEntry>(publishJobs);
    }

    /// <summary>
    ///   Returns publish related jobs in a specified state.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    [NotNull]
    public static IEnumerable<PublishJobEntry> GetJobs(JobState state)
    {
      var jobs = JobManager.GetJobs();
      Assert.IsNotNull(jobs, "jobs");

      var publishJobs = jobs
        .Where(job => job.Category.Equals("publish", StringComparison.InvariantCultureIgnoreCase) && job.Status.State == state)
        .Select(job => new PublishJobEntry(job.Handle, job.Name, job.Category, job.Status, job.Options.ContextUser));

      return new List<PublishJobEntry>(publishJobs);
    }

    /// <summary>
    ///   Returns a selected publish job from the Listview control.
    /// </summary>
    /// <param name="jobContainerId">The Id of the Listview control.</param>
    /// <returns></returns>
    [CanBeNull]
    public static Job GetSelectedJob([NotNull] string jobContainerId)
    {
      Assert.ArgumentNotNull(jobContainerId, "jobContainerId");

      var jobList = Context.ClientPage.FindSubControl(jobContainerId) as Listview;
      if (jobList == null || jobList.SelectedItems.Length <= 0)
      {
        return null;
      }

      var jobHandle = jobList.SelectedItems[0].ID;
      var job = JobManager.GetJob(Handle.Parse(jobHandle));

      return job;
    }

    #endregion

    #region Private methods

    /// <summary>
    ///   Finishes queued job.
    /// </summary>
    /// <param name="job">Job instance to finish.</param>
    private static void FinishJob([NotNull] Job job)
    {
      Assert.ArgumentNotNull(job, "job");

      var type = typeof(JobManager);

      // Use _queuedJobs collection to remove the job from the queue.
      var queuedJobsField = type.GetField("_queuedJobs", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);

      // Use private method of JobManager class to finish the job.
      var finishJobMethod = type.GetMethod("FinishJob", BindingFlags.Static | BindingFlags.NonPublic);
      if (queuedJobsField == null || finishJobMethod == null)
      {
        return;
      }

      var queuedJobs = queuedJobsField.GetValue(null) as JobCollection;
      if (queuedJobs == null)
      {
        return;
      }

      // Remove the job from queuedJobs collection.
      queuedJobs.Remove(job);
      queuedJobsField.SetValue(null, queuedJobs);

      // Move the job to finishedJobs collection.
      finishJobMethod.Invoke(null, new object[] { job });
    }

    #endregion
  }
}