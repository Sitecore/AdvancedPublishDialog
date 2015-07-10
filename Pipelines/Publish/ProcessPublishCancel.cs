namespace Sitecore.SharedSource.Publishing.Pipelines.Publish
{
  using System;
  using Sitecore.Data.Items;
  using Sitecore.Publishing.Pipelines.Publish;

  public class ProcessPublishCancel : PublishProcessor
  {
    private const string PublishCancelBehavior = "{19A6A7BC-09B3-4FF6-9132-5FDD823C7B63}";

    private Item GetSettingItem(string settingId, PublishContext context)
    {
      return context.PublishOptions.SourceDatabase.SelectSingleItem(settingId);
    }

    protected virtual bool IsHardStop(PublishContext context)
    {
      Item settingItem = this.GetSettingItem("{19A6A7BC-09B3-4FF6-9132-5FDD823C7B63}", context);
      return settingItem != null && settingItem["Enable Hard Stop"] == "1";
    }

    public override void Process(PublishContext context)
    {
      if ((context.CustomData.ContainsKey("IsPublishCanceled") ? ((bool)context.CustomData["IsPublishCanceled"]) : false) && this.IsHardStop(context))
      {
        throw new Exception("Publishing has been stopped.");
      }
    }
  }
}

