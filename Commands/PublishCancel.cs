namespace Sitecore.SharedSource.Publishing.Commands
{
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Jobs;
  using Sitecore.SharedSource.Publishing.Classes;
  using Sitecore.Shell.Framework.Commands;
  using Sitecore.Web.UI.Sheer;

  /// <summary>
  ///   Class represents a publish cancel command.
  /// </summary>
  [UsedImplicitly]
  public class PublishCancel : Command
  {
    #region Public methods

    /// <summary>
    ///   Cancels all publishing jobs currently running or in queued state.
    /// </summary>
    public void CancelAll()
    {
      var canceledJobsCount = 0;
      foreach (var job in PublishJobHelper.GetJobs())
      {
        if (job == null || job.Status.State == JobState.Finished)
        {
          continue;
        }

        var jobHandle = job.JobHandle;
        Assert.IsNotNull(jobHandle, "jobHandle");

        PublishJobHelper.CancelJob(this, jobHandle);
        canceledJobsCount++;
      }

      Log.Info(string.Format("Publish cancel: {0} publishing related jobs were cancelled. User: {1}", canceledJobsCount, Sitecore.Context.User.Name), this);
    }

    public override void Execute([NotNull] CommandContext context)
    {
      Assert.IsNotNull(context, "context");

      Context.ClientPage.Start(this, "Run", context.Parameters);
    }

    /// <summary>
    ///   Method gets called in the pipeline to allow an interaction with a user.
    /// </summary>
    /// <param name="args"></param>
    [UsedImplicitly]
    public void Run([NotNull] ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var parameters = args.Parameters;
      Assert.IsNotNull(parameters, "parameters");

      var cancelAll = parameters["cancelAll"];
      if (args.Result == "no")
      {
        args.AbortPipeline();
      }
      else if (args.Result == "yes")
      {
        if (cancelAll == "yes")
        {
          this.CancelAll();
        }
        else
        {
          this.CancelJob();
        }
      }
      else
      {
        if (cancelAll == "yes")
        {
          var jobs = PublishJobHelper.GetJobs(JobState.Running);
          if (jobs.GetEnumerator().MoveNext())
          {
            SheerResponse.Confirm(Translate.Text("Are you sure you want to cancel all current publishing jobs?"));
          }
          else
          {
            SheerResponse.Alert(Translate.Text("There are no publishing jobs to cancel."));
          }
        }
        else
        {
          if (PublishJobHelper.GetSelectedJob("JobList") != null)
          {
            SheerResponse.Confirm(Translate.Text("Are you sure you want to cancel selected publishing job?"));
          }
          else
          {
            SheerResponse.Alert(Translate.Text("Please select a job from the list to cancel."));
          }
        }
      }

      args.WaitForPostBack();
    }

    #endregion

    #region Protected methods

    /// <summary>
    ///   Cancels a selected publishing job in the
    /// </summary>
    protected void CancelJob()
    {
      var job = PublishJobHelper.GetSelectedJob("JobList");
      if (job == null)
      {
        return;
      }

      if (job.Status.State == JobState.Finished)
      {
        SheerResponse.Alert(Translate.Text("This job has already been completed."));
      }
      else
      {
        PublishJobHelper.CancelJob(this, job);
      }
    }

    #endregion
  }
}