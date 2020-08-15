using System;
using Newtonsoft.Json;

namespace ThePathBot.Models
{
    public class FishModel
    {
        [JsonProperty("uniqueEntryID")]
        public string UniqueEntryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unlockCatches")]
        public long UnlockCatches { get; set; }

        [JsonProperty("spawnRate")]
        public string SpawnRate { get; set; }

        [JsonProperty("rainSnowCatchUp")]
        public string RainSnowCatchUp { get; set; }

        [JsonProperty("nhJan")]
        public string NhJan { get; set; }

        [JsonProperty("nhFeb")]
        public string NhFeb { get; set; }

        [JsonProperty("nhMar")]
        public string NhMar { get; set; }

        [JsonProperty("nhApr")]
        public string NhApr { get; set; }

        [JsonProperty("nhMay")]
        public string NhMay { get; set; }

        [JsonProperty("nhJun")]
        public string NhJun { get; set; }

        [JsonProperty("nhJul")]
        public string NhJul { get; set; }

        [JsonProperty("nhAug")]
        public string NhAug { get; set; }

        [JsonProperty("nhSep")]
        public string NhSep { get; set; }

        [JsonProperty("nhOct")]
        public string NhOct { get; set; }

        [JsonProperty("nhNov")]
        public string NhNov { get; set; }

        [JsonProperty("nhDec")]
        public string NhDec { get; set; }

        [JsonProperty("shJan")]
        public string ShJan { get; set; }

        [JsonProperty("shFeb")]
        public string ShFeb { get; set; }

        [JsonProperty("shMar")]
        public string ShMar { get; set; }

        [JsonProperty("shApr")]
        public string ShApr { get; set; }

        [JsonProperty("shMay")]
        public string ShMay { get; set; }

        [JsonProperty("shJun")]
        public string ShJun { get; set; }

        [JsonProperty("shJul")]
        public string ShJul { get; set; }

        [JsonProperty("shAug")]
        public string ShAug { get; set; }

        [JsonProperty("shSep")]
        public string ShSep { get; set; }

        [JsonProperty("shOct")]
        public string ShOct { get; set; }

        [JsonProperty("shNov")]
        public string ShNov { get; set; }

        [JsonProperty("shDec")]
        public string ShDec { get; set; }

        [JsonProperty("color1")]
        public string Color1 { get; set; }

        [JsonProperty("color2")]
        public string Color2 { get; set; }

        [JsonProperty("lightingType")]
        public string LightingType { get; set; }

        [JsonProperty("iconFilename")]
        public string IconFilename { get; set; }

        [JsonProperty("critterpediaFilename")]
        public string CritterpediaFilename { get; set; }

        [JsonProperty("furnitureFilename")]
        public string FurnitureFilename { get; set; }

        [JsonProperty("internalID")]
        public long InternalId { get; set; }

        [JsonProperty("shadow")]
        public string Shadow { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }

        [JsonProperty("sell")]
        public long Sell { get; set; }

        [JsonProperty("whereOrHow")]
        public string WhereOrHow { get; set; }
    }
}
