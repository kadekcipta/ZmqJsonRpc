using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MSA.Zmq.JsonRpc
{
    public sealed class JsonRpcResponse
    {
        public static JsonRpcResponse CreateJsonErrorResponse(int id, int code, string message, string data)
        {
            var error = new JsonRpcError();
            error.Code = code;
            error.Data = data;
            error.Message = message;

            var resp = new JsonRpcResponse();
            resp.JsonRpc = "2.0";
            resp.Id = id;
            resp.Error = error;
            resp.Result = null;

            return resp;
        }

        public static string CreateJsonError(int id, int code, string message, string data)
        {
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.NullValueHandling = NullValueHandling.Ignore;

            return JsonConvert.SerializeObject(CreateJsonErrorResponse(id, code, message, data), Formatting.Indented, jsonSettings);
        }

        public static string CreateJsonResponse(int id, Object result)
        {
            var resp = new JsonRpcResponse();
            resp.JsonRpc = "2.0";
            resp.Id = id;
            resp.Error = null;
            resp.Result = result;

            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.NullValueHandling = NullValueHandling.Ignore;

            return JsonConvert.SerializeObject(resp, Formatting.Indented, jsonSettings);
        }

        public string JsonRpc { get; set; }
        public object Result { get; set; }
        public object Error { get; set; }
        public int Id { get; set; }
    }
}
