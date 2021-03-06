﻿#r "Newtonsoft.Json"
#r "..\bin\AzureSkyMedia.PlatformServices.dll"

using System;
using System.Net;

using Newtonsoft.Json;

using AzureSkyMedia.PlatformServices;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    string webMessage = await req.Content.ReadAsStringAsync();
    log.Info($"Web Message: {webMessage}");
    if (webMessage.StartsWith("{"))
    {
        MediaJobNotification jobNotification = JsonConvert.DeserializeObject<MediaJobNotification>(webMessage);
        if (jobNotification != null)
        {
            string jobPublication = MediaClient.PublishJob(jobNotification, true);
            log.Info($"Job Publication: {jobPublication}");
        }
    }
    return req.CreateResponse(HttpStatusCode.OK);
}
