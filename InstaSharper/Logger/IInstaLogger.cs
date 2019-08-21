using System;
using System.Net.Http;

namespace InstaSharper.Logger
{
    public interface IInstaLogger
    {
        void LogRequest(HttpRequestMessage request);
        void LogRequest(Uri uri);
        void LogResponse(HttpResponseMessage response);
        void LogException(Exception exception);
        void LogException(Exception exception, string message);
        void LogInfo(string info);
    }
}