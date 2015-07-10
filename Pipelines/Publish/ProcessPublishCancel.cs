namespace Sitecore.SharedSource.Publishing.Pipelines.Publish
{
  using System;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Publishing.Pipelines.Publish;

  [UsedImplicitly]
  public class ProcessPublishCancel : PublishProcessor
  {
    #region Constants

    private const string PublishCancelBehavior = "{19A6A7BC-09B3-4FF6-9132-5FDD823C7B63}";

    #endregion

    #region Public methods

    public override void Process([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      var customData = context.CustomData;
      Assert.IsNotNull(customData, "customData");

      if (customData.ContainsKey("IsPublishCanceled") && (bool)customData["IsPublishCanceled"] && this.IsHardStop(context))
      {
        throw new Exception("Publishing has been stopped.");
      }
    }

    #endregion

    #region Protected methods

    protected virtual bool IsHardStop([NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(context, "context");

      Item settingItem = this.GetSettingItem(PublishCancelBehavior, context);
      return settingItem != null && settingItem["Enable Hard Stop"] == "1";
    }

    #endregion

    #region Private methods

    [CanBeNull]
    private Item GetSettingItem([NotNull] string settingId, [NotNull] PublishContext context)
    {
      Assert.ArgumentNotNull(settingId, "settingId");
      Assert.ArgumentNotNull(context, "context");

      return context.PublishOptions.SourceDatabase.SelectSingleItem(settingId);
    }

    #endregion
  }
}