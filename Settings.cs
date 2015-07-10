namespace Sitecore.SharedSource.Publishing
{
  using Sitecore.Data;
  using Sitecore.Diagnostics;

  /// <summary>
  ///   Represents settings for this component.
  /// </summary>
  public static class Settings
  {
    #region Constants

    /// <summary>
    ///   Cancel button setting item path: /sitecore/system/Settings/Publish/Behaviors/Cancel Button
    /// </summary>
    private const string CancelButtonSetting = "{89BA6C1B-C1E8-48BD-A231-A621D7BC1941}";

    /// <summary>
    ///   Confirmation setting item path: /sitecore/system/Settings/Publish/Behaviors/Confirm subitems publish
    /// </summary>
    private const string ConfirmationSetting = "{85901D1A-2F48-456A-8883-F19DA6D478A0}";

    /// <summary>
    ///   Incremental option setting item: /sitecore/system/Settings/Publish/Publish Options/Incremental
    /// </summary>
    private const string IncrementalSetting = "{EB5A39E4-AFFB-4FED-A651-2541D477F743}";

    /// <summary>
    ///   Publish cancel behavior setting item: /sitecore/system/Settings/Publish/Behaviors/Publish Cancel Behavior.
    ///   If hard stop is enabled and a user cancels publishing operation, it throws an exception to prevent publishing job
    ///   from updating Last Publish property and firing publish:end event.
    ///   Otherwise it allows publishing job to update Last Publish property which makes next inremental publish ineffective.
    /// </summary>
    private const string PublishCancelBehavior = "{19A6A7BC-09B3-4FF6-9132-5FDD823C7B63}";

    /// <summary>
    ///   Incremental option setting item: /sitecore/system/Settings/Publish/Publish Options/Publish Children
    /// </summary>
    private const string PublishChildrenSetting = "{4B413FC5-D746-4258-BA23-F06A38F2EF63}";

    /// <summary>
    ///   Republish option setting item: /sitecore/system/Settings/Publish/Publish Options/Republish
    /// </summary>
    private const string RepublishSetting = "{BCE207EA-66C8-4E2C-B8B9-38E4F28F89DE}";

    /// <summary>
    ///   Smart option setting item: /sitecore/system/Settings/Publish/Publish Options/Smart
    /// </summary>
    private const string SmartSetting = "{5AB27381-14A5-45F4-897F-984113B15E14}";

    #endregion

    #region Public properties

    /// <summary>
    ///   Gets ID of Cancel button setting item.
    /// </summary>
    [NotNull]
    public static ID CancelButtonSettingId
    {
      get
      {
        return ParseId(CancelButtonSetting);
      }
    }

    /// <summary>
    ///   Gets ID of Confirm subitems publish setting item.
    /// </summary>
    [NotNull]
    public static ID ConfirmationSettingId
    {
      get
      {
        return ParseId(ConfirmationSetting);
      }
    }

    /// <summary>
    ///   Gets ID of Incremental setting item.
    /// </summary>
    [NotNull]
    public static ID IncrementalSettingId
    {
      get
      {
        return ParseId(IncrementalSetting);
      }
    }

    /// <summary>
    ///   Gets ID PublishCancelBehavior setting item.
    /// </summary>
    [NotNull]
    public static ID PublishCancelBehaviorId
    {
      get
      {
        return ParseId(PublishCancelBehavior);
      }
    }

    /// <summary>
    ///   Gets ID of Publish Children setting item.
    /// </summary>
    [NotNull]
    public static ID PublishChildrenSettingId
    {
      get
      {
        return ParseId(PublishChildrenSetting);
      }
    }

    /// <summary>
    ///   Gets ID of Republish setting item.
    /// </summary>
    [NotNull]
    public static ID RepublishSettingId
    {
      get
      {
        return ParseId(RepublishSetting);
      }
    }

    /// <summary>
    ///   Gets ID of Smart setting item.
    /// </summary>
    [NotNull]
    public static ID SmartSettingId
    {
      get
      {
        return ParseId(SmartSetting);
      }
    }

    #endregion

    #region Private methods

    [NotNull]
    private static ID ParseId([NotNull] string str)
    {
      Assert.ArgumentNotNull(str, "str");

      ID id;
      Assert.IsTrue(ID.TryParse(str, out id), "The specified ID is not valid: " + str);
      Assert.IsNotNull(id, "id");

      return id;
    }

    #endregion
  }
}