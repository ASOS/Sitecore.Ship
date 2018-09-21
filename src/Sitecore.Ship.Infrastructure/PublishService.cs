
using System;
using System.Linq;

using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using Sitecore.Ship.Core.Contracts;
using Sitecore.Ship.Core.Domain;

namespace Sitecore.Ship.Infrastructure
{
    public class PublishService : IPublishService
    {
        public void AddToPublishQueue(Guid itemId)
        {
            var master = Sitecore.Configuration.Factory.GetDatabase("master");
            Sitecore.Publishing.PublishManager.AddToPublishQueue(master, new ID(itemId), ItemUpdateType.Saved, DateTime.UtcNow);
        }

        public void Run(ItemsToPublish itemsToPublish)
        {
            if (itemsToPublish == null)
            {
                throw new ArgumentNullException("itemsToPublish");
            }

            if (itemsToPublish.Items.Count == 0)
            {
                return;
            }

            using (new SecurityModel.SecurityDisabler())
            {
                var master = Sitecore.Configuration.Factory.GetDatabase("master");
                var languages = itemsToPublish.TargetLanguages.Select(LanguageManager.GetLanguage).ToArray();

                foreach (var itemToPublish in itemsToPublish.Items)
                {
                    var item = master.GetItem(new ID(itemToPublish));
                    if (item != null)
                    {
                        Publishing.PublishManager.PublishItem(item, itemsToPublish.TargetDatabases.Select(Sitecore.Configuration.Factory.GetDatabase).ToArray(), languages, true, true);
                    }
                }
            }
        }

        public void Run(PublishParameters publishParameters)
        {
            var publishingMode = publishParameters.Mode.ToLower();

            PublishingTask(publishingMode, publishParameters);
        }

        public DateTime GetLastCompletedRun(PublishLastCompleted completeParameters)
        {
            // please note http://stackoverflow.com/questions/12416141/get-the-date-time-that-sitecore-last-published

            var source = Sitecore.Configuration.Factory.GetDatabase(completeParameters.Source);
            var target = Sitecore.Configuration.Factory.GetDatabase(completeParameters.Target);

            var language = LanguageManager.GetLanguage(completeParameters.Language);


            Assert.IsNotNull(source, "Source database {0} cannot be found".Formatted(completeParameters.Source));
            Assert.IsNotNull(source, "Target database {0} cannot be found".Formatted(completeParameters.Target));
            Assert.IsNotNull(language, "Language {0} cannot be found".Formatted(completeParameters.Language));

            var date = source.Properties.GetLastPublishDate(target, language);
            return date;
        }

        private static void PublishingTask(string publishType, PublishParameters publishParameters)
        {
            using (new SecurityModel.SecurityDisabler())
            {
                var master = Sitecore.Configuration.Factory.GetDatabase(publishParameters.Source);
                var targetDBs = publishParameters.Targets.Select(Sitecore.Configuration.Factory.GetDatabase).ToArray();
                var languages = publishParameters.Languages.Length > 0 ? publishParameters.Languages.Select(LanguageManager.GetLanguage).ToArray() : LanguageManager.GetLanguages(master).ToArray();
                foreach (var target in targetDBs)
                {
                    var options = new PublishOptions(master, target, ParseMode(publishType), languages[0], DateTime.Now)
                    { Deep = publishParameters.Deep, PublishRelatedItems = publishParameters.Related };
                    var publisher = new Publisher(options, languages);
                    publisher.PublishAsync();
                }
            }
        }
        private static PublishMode ParseMode(string mode)
        {
            if (mode.Equals("Full", StringComparison.InvariantCultureIgnoreCase))
            {
                return PublishMode.Full;
            }

            if (mode.Equals("Incremental", StringComparison.InvariantCultureIgnoreCase))
            {
                return PublishMode.Incremental;
            }

            if (mode.Equals("Smart", StringComparison.InvariantCultureIgnoreCase))
            {
                return PublishMode.Smart;
            }

            throw new InvalidOperationException(string.Format("Invalid publishing mode ({0})", mode));
        }
    }
}
