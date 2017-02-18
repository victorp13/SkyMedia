using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.WindowsAzure.MediaServices.Client;

namespace AzureSkyMedia.PlatformServices
{
    public static class StreamFile
    {
        private static MediaTrack[] GetTextTracks(MediaClient mediaClient, IAsset asset, LocatorType locatorType)
        {
            List<MediaTrack> textTracks = new List<MediaTrack>();
            string fileExtension = Constants.Media.AssetFile.VttExtension;
            string[] fileNames = MediaClient.GetFileNames(asset, fileExtension);
            foreach (string fileName in fileNames)
            {
                MediaTrack textTrack = new MediaTrack();
                textTrack.Type = Constants.Media.Stream.TextTrackSubtitles;
                textTrack.Source = mediaClient.GetLocatorUrl(asset, locatorType, fileName);
                textTrack.Language = GetLanguageCode(textTrack.Source);
                textTracks.Add(textTrack);
            }
            return textTracks.ToArray();
        }

        private static string GetAnalyticsProcessorName(IAssetFile assetFile)
        {
            string[] fileNameInfo = assetFile.Name.Split('_');
            string processorName = fileNameInfo[fileNameInfo.Length - 1];
            processorName = processorName.Replace(Constants.Media.AssetFile.JsonExtension, string.Empty);
            return processorName.Replace(Constants.NamedItemSeparator, ' ');
        }

        private static MediaMetadata[] GetAnalyticsProcessors(IAsset asset)
        {
            List<MediaMetadata> analyticsProcessors = new List<MediaMetadata>();
            foreach (IAssetFile assetFile in asset.AssetFiles)
            {
                if (assetFile.Name.EndsWith(Constants.Media.AssetFile.JsonExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    string processorName = GetAnalyticsProcessorName(assetFile);
                    MediaMetadata mediaMetadata = new MediaMetadata();
                    mediaMetadata.ProcessorName = processorName;
                    mediaMetadata.MetadataFile = assetFile.Name;
                    analyticsProcessors.Add(mediaMetadata);
                }
            }
            return analyticsProcessors.ToArray();
        }

        private static int OrderLocators(ILocator leftSide, ILocator rightSide)
        {
            return DateTime.Compare(leftSide.Asset.Created, rightSide.Asset.Created);
        }

        public static string GetLanguageCode(string sourceUrl)
        {
            string[] sourceInfo = sourceUrl.Split('.');
            string fileName = sourceInfo[sourceInfo.Length - 2];
            return fileName.Substring(fileName.Length - 2);
        }

        public static MediaStream[] GetMediaStreams(MediaClient mediaClient)
        {
            List<MediaStream> mediaStreams = new List<MediaStream>();
            string settingKey = Constants.AppSettingKey.MediaLocatorMaxStreamCount;
            int maxStreamCount = int.Parse(AppSetting.GetValue(settingKey));
            ILocator[] locators = mediaClient.GetEntities(MediaEntity.Locator) as ILocator[];
            Array.Sort<ILocator>(locators, OrderLocators);
            foreach (ILocator locator in locators)
            {
                IAsset asset = locator.Asset;
                if (asset.IsStreamable && asset.AssetFiles.Count() > 1)
                {
                    string locatorUrl = mediaClient.GetLocatorUrl(asset, locator.Type, null);
                    if (!string.IsNullOrEmpty(locatorUrl))
                    {
                        MediaStream mediaStream = new MediaStream();
                        mediaStream.Name = asset.Name;
                        mediaStream.SourceUrl = locatorUrl;
                        mediaStream.TextTracks = GetTextTracks(mediaClient, asset, locator.Type);
                        mediaStream.ProtectionTypes = mediaClient.GetProtectionTypes(asset);
                        mediaStream.AnalyticsProcessors = GetAnalyticsProcessors(asset);
                        mediaStreams.Add(mediaStream);
                    }
                }
                if (mediaStreams.Count == maxStreamCount)
                {
                    break;
                }
            }
            return mediaStreams.ToArray();
        }
    }
}