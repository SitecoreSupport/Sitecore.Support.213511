namespace Sitecore.Support.Forms.Shell.UI.Dialogs
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;
  using Sitecore.Form.Core.Utility;
  using Sitecore.Form.Web.UI.Controls;
  using Sitecore.Forms.Core.Data;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.WebControls;
  using Sitecore.Web.UI.XamlSharp.Xaml;
  using SendStandardMessagePageDefault = Sitecore.Forms.Shell.UI.Dialogs.SendStandardMessagePage;

  public class SendStandardMessagePage : SendStandardMessagePageDefault
  {
    private List<string> tokensList;

    protected override bool ActivePageChanging(string page, ref string newpage)
    {
      if ((page == "SelectMessagePage") && (newpage == "PersonalizeMessagePage"))
      {
        if (this.MessageLink.Value.Length == 0)
        {
          XamlControl.AjaxScriptManager.Alert(Texts.Localize("Select the triggered message that you want to use.", new object[0]));
          return false;
        }
        if (this.Page.Request.Form["UseVisitor"] == "UseVisitor")
        {
          if (string.IsNullOrEmpty(this.NameField.GetEnabledSelectedValue()))
          {
            XamlControl.AjaxScriptManager.Alert(Texts.Localize("The user name is required.", new object[0]));
            return false;
          }
          if (string.IsNullOrEmpty(this.DomainField.GetEnabledSelectedValue()))
          {
            XamlControl.AjaxScriptManager.Alert(Texts.Localize("The domain is required.", new object[0]));
            return false;
          }
        }
        if (this.GetMessageTokens().Any<string>())
        {
          this.GenerateTokens();
          XamlControl.AjaxScriptManager.SetOuterHtml(this.TokensPanel.ID, this.TokensPanel);
        }
        else
        {
          newpage = "PreviewMessagePage";
        }
      }
      if (((page == "PreviewMessagePage") && (newpage == "PersonalizeMessagePage")) && !this.GetMessageTokens().Any<string>())
      {
        newpage = "SelectMessagePage";
      }
      if (page == "PersonalizeMessagePage")
      {
        this.AddCustomPersonTokens();
        this.TokensValue.Value = Utils.DictionaryToString(this.Message.CustomPersonTokens, '&');
        XamlControl.AjaxScriptManager.SetAttribute(this.TokensValue.ClientID, "value", this.TokensValue.Value);
      }
      if (newpage == "PreviewMessagePage")
      {
        this.GeneratePreview();
      }
      return true;
    }

    private void AddCustomPersonTokens()
    {
      foreach (string str in this.GetMessageTokens())
      {
        string str2 = this.Page.Request.Form["it_" + str];
        if (this.Message.CustomPersonTokens.ContainsKey(str))
        {
          this.Message.CustomPersonTokens[str] = str2;
        }
        else
        {
          this.Message.CustomPersonTokens.Add(str, str2);
        }
      }
    }

    private IEnumerable<string> GetMessageTokens()
    {
      if (this.tokensList != null)
      {
        return this.tokensList;
      }

      this.tokensList = new List<string>();
      if (this.Message == null)
      {
        return this.tokensList;
      }

      var profileTokens = Sitecore.Support.Form.Core.EcmClientApiWrapper.Instance.GetProfileTokens(this.Message);
      var messageTokens = Sitecore.Support.Form.Core.EcmClientApiWrapper.Instance.GetMessageTokens(this.Message);
      this.tokensList = messageTokens.Where(token => !profileTokens.Contains(token)).ToList();
      this.tokensList.Sort();
      return this.tokensList;
    }
    protected void GeneratePreview()
    {
      if (!GlobalSettings.HasAccess)
      {
        this.MessageBody.InnerHtml =
          MessageInfo.RenderBodyWarningMessage(
            Texts.Localize(Texts.GlobalSettingsItemMissedOrNoRights),
            string.Empty,
            string.Empty,
            string.Empty,
            false);
        return;
      }

      this.AddCustomPersonTokens();
      Sitecore.Support.Form.Core.EcmClientApiWrapper.Instance.SetPersonalizationContact(this.Message);

      MessageInfo info = CoreFactory.Instance.GetMessageInfo(this.Message);
      info.FillContentEditorInfo();

      this.Subject.Text = info.Subject;
      this.From.Text = info.From;

      bool first = string.IsNullOrEmpty(this.HtmlBody.Text);
      this.HtmlBody.Text = HttpUtility.HtmlEncode(info.Body);

      if (first)
      {
        this.MessageBody.InnerHtml = this.CorrectBody("Sitecore.Wfm.MessagePreview.Initialize()");
        return;
      }

      AjaxScriptManager.SetOuterHtml(
        this.MessageBody.ClientID,
        string.Format("<div id=\"{0}\" class=\"{1}\">{2}</div>", this.MessageBody.ClientID, this.MessageBody.Attributes["class"], this.CorrectBody("Sitecore.Wfm.MessagePreview.AddBody()")));
      AjaxScriptManager.Eval("setTimeout(Sitecore.Wfm.MessagePreview.AddBody, 5);");
    }

    private string CorrectBody(string handler)
    {
      Frame frame = new Frame
      {
        ID = "BodyFrame"
      };

      frame.Attributes["onload"] = handler;
      string body = frame.RenderAsText();
      return body.Trim();
    }
  }
}