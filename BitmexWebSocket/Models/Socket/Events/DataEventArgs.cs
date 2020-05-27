using BitmexWebSocket.Dtos.Socket;
using Newtonsoft.Json.Linq;
using System;

namespace BitmexWebSocket.Models.Socket.Events
{
    public class DataEventArgs : EventArgs
    {
        public string TableName { get; }
        public BitmexActions Action { get; }
        public JToken Data { get; }

        public DataEventArgs(string tableName, JToken data, BitmexActions action)
        {
            TableName = tableName;
            Data = data;
            Action = action;
        }
    }
}
