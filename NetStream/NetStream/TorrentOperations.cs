using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetStream
{
    public enum TorrentOperationType
    {
        Added,
        Deleted,
        Paused,
        Resumed,
        ProgressUpdated,
        TorrentList
    }
    
    public class TorrentOperations
    {
        public TorrentOperationType OperationType { get; set; }
        public Item Item { get; set; }
        public List<Item> ItemList { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, 
                new JsonSerializerSettings 
                { 
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore 
                });
        }
        
        public static TorrentOperations FromJson(string json)
        {
            return JsonConvert.DeserializeObject<TorrentOperations>(json);
        }
    }
} 