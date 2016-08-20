using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace OpenHeroesUploader
{


    public class Uploader
    {
        public Uri UploadUri { get; }
        private SemaphoreSlim Throttle { get; }

        public Uploader(Uri uploadUri, SemaphoreSlim throttle)
        {
            Throttle = throttle;
            UploadUri = uploadUri;
        }

        public async Task<ReplayForUpload> UploadWithRetries(ReplayForUpload replay, int retries, int delayms)
        {
            var newdelay = (delayms <= 0) ? 500 : 2 * delayms;
            await Task.Delay(delayms);
            if (retries <= 1)
            {
                return await UploadReplay(replay);
            } else
            {
                try
                {
                    return await UploadReplay(replay);
                } catch (Exception e)
                {
                    return await UploadWithRetries(replay, retries - 1, newdelay);
                }
            }
        }

        private async Task<ReplayForUpload> UploadReplay(ReplayForUpload replay)
        {
            try
            {
                await UploadReplay(replay.Path);
                replay.State = UploadState.Uploaded;
                return replay;
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Failed to upload replay: {0} - {0}",e.GetType(), e.Message));
                replay.State = UploadState.Faulted;
                return replay;
            }
        }

        private async Task<HttpResponseMessage> UploadReplay(String path)
        {
            await Throttle.WaitAsync();
            try
            {
                using (var filestream = File.OpenRead(path))
                {
                    var client = new HttpClient();
                    client.BaseAddress = UploadUri;
                    var response = await client.PostAsync(UploadUri, new StreamContent(filestream));
                    if (response.IsSuccessStatusCode) return response;
                    else throw new Exception("sad panda");
                }
            } finally
            {
                Throttle.Release();
            }
        }
    }
}
