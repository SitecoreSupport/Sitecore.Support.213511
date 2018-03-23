namespace Sitecore.Support.Form.Core
{
  using Sitecore;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core;
  using Sitecore.Modules.EmailCampaign.Messages;
  using Sitecore.Modules.EmailCampaign.Recipients;
  using Sitecore.Modules.EmailCampaign.UI;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using EcmClientApiWrapperDefault = Sitecore.Form.Core.EcmClientApiWrapper;

  public class EcmClientApiWrapper : EcmClientApiWrapperDefault
  {
    private static EcmClientApiWrapper instance;

    static EcmClientApiWrapper()
    {
      instance = new Sitecore.Support.Form.Core.EcmClientApiWrapper();
    }

    protected EcmClientApiWrapper()
    {
    }

    public new static EcmClientApiWrapper Instance
    {
      get
      {
        return instance;
      }

      set
      {
        Assert.ArgumentNotNull(value, "value");
        instance = value;
      }
    }

    public new static bool IsEcmInstalled
    {
      get
      {
        try
        {
          new ClientApi();
          return true;
        }
        catch (Exception)
        {
          return false;
        }
      }
    }

    public override List<ManagerRoot> GetAllManagerRoots()
    {
      return Factory.Instance.GetManagerRoots();
    }

    public override Contact GetContactFromName(string name)
    {
      return Factory.GetContactFromName(name);
    }

    public override ManagerRoot GetManagerRootFromChildItem(Item messageItem)
    {
      return Factory.GetManagerRootFromChildItem(messageItem);
    }

    public override MessageItem GetMessage(string messageId)
    {
      return Factory.GetMessage(messageId);
    }

    public override List<string> GetMessageTokens(MessageItem message)
    {
      var tokens = new List<string>();
      var matchesList = this.GetAbnMessageTokens(message as WebPageMail);
      if (matchesList.Count == 0)
      {
        matchesList.Add(Regex.Matches(message.Subject, @"\$\w+\$", RegexOptions.IgnoreCase));
        matchesList.Add(Regex.Matches(this.GetMessageBody(message), @"\$\w+\$", RegexOptions.IgnoreCase));
      }

      foreach (var token in matchesList.SelectMany(matches => matches.Cast<Match>().Select(match => match.Value.Trim('$')).Where(token => !tokens.Contains(token))))
      {
        tokens.Add(token);
      }

      return tokens;
    }

    public override List<string> GetProfileTokens(MessageItem message)
    {
      var profileTokens = new List<string>();
      var root = this.GetManagerRootFromChildItem(message.InnerItem);
      if (root != null)
      {
        profileTokens.AddRange(root.GetDefaultUserProperties().Keys);
      }

      foreach (var key in new[] { "contact", "profile", "name", "fullname", "email", "phone", "title" }.Where(key => !profileTokens.Contains(key)))
      {
        profileTokens.Add(key);
      }

      return profileTokens;
    }

    public override string GetMessageBody(MessageItem message)
    {
      var webPageMessage = message as WebPageMail;
      if (webPageMessage == null)
      {
        return message.GetMessageBody();
      }

      this.SetPersonalizationContact(webPageMessage);
      webPageMessage.DisplayMode = MessageDisplayMode.Preview;
      return HtmlHelper.DownloadStringContent(webPageMessage.BodyLink);
    }

    public new void SetPersonalizationContact(MessageItem message)
    {
      var contact = Sitecore.Support.Modules.EmailCampaign.UI.PreviewSubscriber.GetRandom(message);
      if (contact != null)
      {
        message.PersonalizationRecipient = contact;
        return;
      }

      if (Context.User != null)
      {
        var repository = RecipientRepository.GetDefaultInstance();
        message.PersonalizationRecipient = repository.GetRecipient(new SitecoreUserName(Context.User.Name));
      }
    }

    protected override List<MatchCollection> GetAbnMessageTokens(WebPageMail message)
    {
      if (message == null)
      {
        return new List<MatchCollection>();
      }

      var abn = CoreFactory.Instance.GetAbnTest(message);
      var testValuesItems = abn == null ? new List<Item>() : abn.TestCandidates;
      var matchesList = new List<MatchCollection>();
      foreach (var testItem in testValuesItems)
      {
        message.TargetItem = testItem;
        var body = this.GetMessageBody(message);
        matchesList.Add(Regex.Matches(((WebPageMailSource)message.Source).Subject, @"\$\w+\$", RegexOptions.IgnoreCase));
        matchesList.Add(Regex.Matches(body, @"\$\w+\$", RegexOptions.IgnoreCase));
        message.TargetItem = null;
      }

      return matchesList;
    }
  }
}