﻿namespace SkyMedia.ServiceBroker
{
    internal enum EntityType
    {
        StorageAccount,
        ContentKey,
        ContentKeyAuthPolicy,
        ContentKeyAuthPolicyOption,
        Manifest,
        ManifestAsset,
        ManifestFile,
        Asset,
        File,
        Channel,
        Program,
        ReservedUnit,
        Processor,
        JobTemplate,
        Job,
        NotificationEndpoint,
        AccessPolicy,
        DeliveryPolicy,
        StreamingEndpoint,
        StreamingFilter,
        Locator
    }

    public enum MediaProcessor
    {
        None,
        EncoderStandard,
        EncoderPremium,
        IndexerV1,
        IndexerV2,
        VideoSummarization,
        CharacterRecognition,
        ObjectDetection,
        FaceDetection,
        FaceRedaction,
        MotionDetection,
        MotionHyperlapse,
        MotionStabilization
    }

    public enum ProtectionType
    {
        AES,
        PlayReady,
        Widevine,
        FairPlay
    }

    public enum TransferService
    {
        SigniantFlight,
        AsperaFasp
    }
}