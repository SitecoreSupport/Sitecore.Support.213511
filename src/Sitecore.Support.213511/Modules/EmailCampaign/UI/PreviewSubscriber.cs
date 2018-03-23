namespace Sitecore.Support.Modules.EmailCampaign.UI
{
  using Sitecore;
  using Sitecore.Diagnostics;
  using Sitecore.Modules.EmailCampaign.Messages;
  using Sitecore.Modules.EmailCampaign.Recipients;
  using System.Collections.Generic;

  public class PreviewSubscriber
  {
    private static Recipient FindRandomRecipient(IList<RecipientId> recipientIds)
    {
      Assert.ArgumentNotNull(recipientIds, "recipientIds");
      foreach (RecipientId id in recipientIds)
      {
        Recipient recipient = RecipientRepository.GetDefaultInstance().GetRecipient(id);
        if (recipient != null)
        {
          CommunicationSettings defaultProperty = recipient.GetProperties<CommunicationSettings>().DefaultProperty;
          if ((defaultProperty == null) || !defaultProperty.IsCommunicationSuspended)
          {
            return recipient;
          }
        }
      }
      return null;
    }

    public static Recipient GetRandom(MessageItem message)
    {
      Assert.ArgumentNotNull(message, "message");
      Recipient recipient = FindRandomRecipient(message.SubscribersIds.Value);
      if (recipient == null)
      {
        return null;
      }
      return recipient;
    }

    public static Recipient GetSubscriber(MessageItem message)
    {
      Recipient random = GetRandom(message);
      if (random != null)
      {
        return random;
      }
      if (Context.User != null)
      {
        return RecipientRepository.GetDefaultInstance().GetRecipient(new SitecoreUserName(Context.User.Name));
      }
      return null;
    }
  }
}