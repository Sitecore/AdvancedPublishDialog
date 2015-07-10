namespace Sitecore.SharedSource.Shell.Applications.Publishing
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.SharedSource.Publishing.Classes;
  using Sitecore.Shell.Framework.Commands;
  using Sitecore.Web;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.Web.UI.WebControls.Ribbons;

  public class PublishStatusManager : BaseForm
  {
    #region Fields

    protected Listview JobList;
    protected Scrollbox JobPanel;
    protected Border RibbonContainer;

    // Window refresh frequency.
    private int Timer = 3000;

    #endregion

    #region Protected methods

    /// <summary>
    ///   Event handler on double-click event in the job list control.
    ///   The event allows to end a selected publishing job.
    /// </summary>
    protected void DblClick_CancelJob()
    {
      Context.ClientPage.SendMessage(this, "publishcontroller:cancel");
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        Assert.CanRunApplication("Publish Status Manager");
        this.RenderRibbon();
      }
    }

    /// <summary>
    ///   Fills out job list and sets a timer to update it.
    /// </summary>
    protected void Populate()
    {
      this.FillJobList();
      SheerResponse.Timer("Populate", this.Timer);
    }

    /// <summary>
    ///   Fills out required fields for a ListviewItem control.
    /// </summary>
    /// <param name="listItem">ListviewItem instance.</param>
    /// <param name="job">A publishing job to be assosiated with.</param>
    /// <param name="selectedItemId">Selected item ID.</param>
    protected void PopulateListviewItem(ListviewItem listItem, PublishJobEntry job, string selectedItemId)
    {
      listItem.ID = job.JobHandle;
      if (job.JobHandle.Equals(selectedItemId))
      {
        listItem.Selected = true;
      }

      listItem.Header = job.Name;
      listItem.ColumnValues["jobName"] = job.Name;
      listItem.ColumnValues["jobCategory"] = job.Category;
      listItem.ColumnValues["jobState"] = job.State;
      listItem.ColumnValues["jobOwner"] = job.OwnerName;
      listItem.ColumnValues["jobProcessed"] = job.Status.Total > 0
        ? job.Status.Processed + "/" + job.Status.Total
        : job.Status.Processed.ToString();

      // QueueTime get setup whenever the job gets created. 
      listItem.ColumnValues["jobStarted"] = job.Status.Job.QueueTime.ToLocalTime().ToLongTimeString();
    }

    /// <summary>
    ///   This method is intended to start an auto update cycle for the application.
    /// </summary>
    protected void StartTimer()
    {
      this.Populate();
    }

    #endregion

    #region Private methods

    /// <summary>
    ///   Adds a dummy message to the job list control if there are no publishing jobs to show.
    /// </summary>
    private void AddEmptyItem()
    {
      this.JobList.Controls.Clear();
      ListviewItem emptyItem = new ListviewItem();
      emptyItem.Header = Sitecore.Globalization.Translate.Text("There are no publishing jobs to display.");
      emptyItem.Disabled = true;
      Context.ClientPage.AddControl(this.JobList, emptyItem);
      SheerResponse.SetInnerHtml("JobPanel", this.JobList);
    }

    /// <summary>
    ///   Adds publising jobs to the job list control.
    /// </summary>
    private void FillJobList()
    {
      IEnumerable<PublishJobEntry> jobEntries = PublishJobHelper.GetJobs();
      if (((IList)jobEntries).Count == 0)
      {
        this.AddEmptyItem();
      }
      else
      {
        string selectedItemId = string.Empty;
        if (this.JobList.SelectedItems.Length > 0)
        {
          selectedItemId = this.JobList.SelectedItems[0].ID;
        }

        this.JobList.Controls.Clear();
        foreach (var job in jobEntries)
        {
          ListviewItem listItem = new ListviewItem();
          Context.ClientPage.AddControl(this.JobList, listItem);
          this.PopulateListviewItem(listItem, job, selectedItemId);
        }

        SheerResponse.SetInnerHtml("JobPanel", this.JobList);
      }
    }

    private void RenderRibbon()
    {
      Ribbon ribbon = new Ribbon()
      {
        ID = "PublishStatusManagerRibbon", 
        ShowContextualTabs = false
      };
      Item itemNotNull = Client.GetItemNotNull("/sitecore/content/Applications/Publish Status Manager/Ribbon", Client.CoreDatabase);
      CommandContext context = new CommandContext();
      context.Parameters.Add("jobHandle", this.JobList.SelectedItems.Length > 0 ? this.JobList.SelectedItems[0].ID : string.Empty);
      context.RibbonSourceUri = itemNotNull.Uri;
      ribbon.CommandContext = context;
      this.RibbonContainer.InnerHtml = HtmlUtil.RenderControl(ribbon);
    }

    #endregion
  }
}