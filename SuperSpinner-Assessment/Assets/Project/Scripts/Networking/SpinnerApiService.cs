using System;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperSpinner.Networking
{
    public sealed class SpinnerApiService
    {
        private const string BaseUrl = "https://platform00.abzorbagames.com/eplatform/spinner";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        public IObservable<SpinnerValuesResponse> GetValues() =>
            SendJsonGet<SpinnerValuesResponse>($"{BaseUrl}/values")
                .Timeout(DefaultTimeout)
                .Retry(1);

        public IObservable<SpinnerSpinResponse> Spin() =>
            SendJsonPost<SpinnerSpinResponse>($"{BaseUrl}/spin", "{}")
                .Timeout(DefaultTimeout)
                .Retry(1);

        private static IObservable<T> SendJsonGet<T>(string url) where T : class
        {
            return Observable.FromCoroutine<T>((observer, ct) => RequestCoroutine(url, UnityWebRequest.kHttpVerbGET, null, observer));
        }

        private static IObservable<T> SendJsonPost<T>(string url, string jsonBody) where T : class
        {
            return Observable.FromCoroutine<T>((observer, ct) => RequestCoroutine(url, UnityWebRequest.kHttpVerbPOST, jsonBody, observer));
        }

        private static System.Collections.IEnumerator RequestCoroutine<T>(
            string url,
            string method,
            string jsonBody,
            IObserver<T> observer
        ) where T : class
        {
            using var req = new UnityWebRequest(url, method);

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Accept", "application/json");

            if (method == UnityWebRequest.kHttpVerbPOST)
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody ?? "{}");
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            var ok = req.result == UnityWebRequest.Result.Success;
#else
            var ok = !req.isNetworkError && !req.isHttpError;
#endif

            if (!ok)
            {
                observer.OnError(new Exception($"Request failed: {method} {url} | {req.responseCode} | {req.error}"));
                yield break;
            }

            var json = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
            {
                observer.OnError(new Exception($"Empty response: {method} {url}"));
                yield break;
            }

            T data;
            try
            {
                data = JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                observer.OnError(new Exception($"JSON parse failed: {method} {url} | {e.Message}\n{json}"));
                yield break;
            }

            if (data == null)
            {
                observer.OnError(new Exception($"Parsed null: {method} {url}\n{json}"));
                yield break;
            }

            observer.OnNext(data);
            observer.OnCompleted();
        }
    }
}
