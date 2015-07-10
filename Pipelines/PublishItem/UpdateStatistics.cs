namespace Sitecore.SharedSource.Publishing.Pipelines.PublishItem
{
  using Sitecore.Diagnostics;
  using Sitecore.Publishing;
  using Sitecore.Publishing.Pipelines.PublishItem;

  /// <summary>
  ///   UpdateJobStatus class
  /// </summary>
  [UsedImplicitly]
  public class UpdateStatistics : PublishItemProcessor
  {
    #region Public properties

    /// <summary>
    ///   Gets or sets a value indicating whether to trace publishing information for every item to the log.
    /// </summary>
    /// <value><c>true</c> if trace to log; otherwise, <c>false</c>.</value>
    public virtual bool TraceToLog { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    ///   Processes the specified args.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void Process([NotNull] PublishItemContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      this.UpdateContextStatistics(context);
      this.UpdateJobStatistics(context);
      this.TraceInformation(context);
    }

    #endregion

    #region Private methods

    /// <summary>
    ///   Traces the information.
    /// </summary>
    /// <param name="context">The context.</param>
    private void TraceInformation([NotNull] PublishItemContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      if (!this.TraceToLog)
      {
        return;
      }

      var result = context.Result;
      var item = context.PublishHelper.GetItemToPublish(context.ItemId);

      var itemName = item != null ? item.Name : "(null)";
      var itemOperation = result != null ? result.Operation.ToString() : "(null)";
      var childAction = result != null ? result.ChildAction.ToString() : "(null)";
      var explanation = result != null && result.Explanation.Length > 0 ? result.Explanation : "(none)";

      Log.Info("##Publish Item:         " + itemName + " - " + context.ItemId, this);
      Log.Info("##Publish Operation:    " + itemOperation, this);
      Log.Info("##Publish Child Action: " + childAction, this);
      Log.Info("##Explanation:          " + explanation, this);
    }

    /// <summary>
    ///   Updates the publish context.
    /// </summary>
    /// <param name="context">The context.</param>
    private void UpdateContextStatistics([NotNull] PublishItemContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      var result = context.Result;
      var publishContext = context.PublishContext;

      if (result == null || publishContext == null)
      {
        return;
      }

      switch (result.Operation)
      {
        case PublishOperation.None:
        case PublishOperation.Skipped:
          lock (publishContext)
          {
            publishContext.Statistics.Skipped++;
          }

          break;

        case PublishOperation.Created:
          lock (publishContext)
          {
            publishContext.Statistics.Created++;
          }

          break;

        case PublishOperation.Updated:
          lock (publishContext)
          {
            publishContext.Statistics.Updated++;
          }

          break;

        case PublishOperation.Deleted:
          lock (publishContext)
          {
            publishContext.Statistics.Deleted++;
          }

          break;
      }
    }

    /// <summary>
    ///   Updates the job statistics.
    /// </summary>
    /// <param name="context">The context.</param>
    private void UpdateJobStatistics([NotNull] PublishItemContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      var result = context.Result;
      if (result == null || result.Operation == PublishOperation.None)
      {
        return;
      }

      var job = context.Job;
      if (job == null)
      {
        return;
      }

      lock (job)
      {
        job.Status.Processed++;
      }
    }

    #endregion
  }
}