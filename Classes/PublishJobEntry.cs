namespace Sitecore.SharedSource.Publishing.Classes
{
  using Sitecore.Jobs;
  using Sitecore.Security.Accounts;

  /// <summary>
  ///   Class is intended to simplify job properties of a job instance.
  /// </summary>
  public class PublishJobEntry
  {
    #region Constructors

    public PublishJobEntry([CanBeNull] Handle jobHandle, [CanBeNull] string jobName, [CanBeNull] string category, [CanBeNull] JobStatus jobStatus, [CanBeNull] User jobOwner)
    {
      this.JobHandle = jobHandle.ToString();
      this.Name = jobName;
      this.Status = jobStatus;
      this.Owner = jobOwner;
      this.Category = category;
    }

    #endregion

    #region Public properties

    /// <summary>
    ///   Job category.
    /// </summary>
    [CanBeNull]
    public string Category { get; set; }

    /// <summary>
    ///   String value of a job handle property.
    /// </summary>
    [CanBeNull]
    public string JobHandle { get; set; }

    /// <summary>
    ///   Job name.
    /// </summary>
    [CanBeNull]
    public string Name { get; set; }

    /// <summary>
    ///   User account that owns the job.
    /// </summary>
    [CanBeNull]
    public Account Owner { get; set; }

    /// <summary>
    ///   A name of user account.
    /// </summary>
    [CanBeNull]
    public string OwnerName
    {
      get
      {
        return this.Owner != null ? this.Owner.Name : "Unknown";
      }
    }

    /// <summary>
    ///   String value of a job state property.
    /// </summary>
    [CanBeNull]
    public string State
    {
      get
      {
        return this.Status != null ? this.Status.State.ToString() : "Unknown";
      }
    }

    /// <summary>
    ///   Job status object.
    /// </summary>
    [CanBeNull]
    public JobStatus Status { get; set; }

    #endregion
  }
}