using System;
using Newtonsoft.Json;

namespace ThePathBot.Models
{
    public class VillagerModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("color_1")]
        public string Color1 { get; set; }

        [JsonProperty("color_2")]
        public string Color2 { get; set; }

        [JsonProperty("unique_Entry_ID")]
        public string UniqueEntryId { get; set; }

        [JsonProperty("species")]
        public string Species { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("personality")]
        public string Personality { get; set; }

        [JsonProperty("hobby")]
        public string Hobby { get; set; }

        [JsonProperty("birthday")]
        public string Birthday { get; set; }

        [JsonProperty("catchphrase")]
        public string Catchphrase { get; set; }

        [JsonProperty("favorite_Song")]
        public string FavoriteSong { get; set; }

        [JsonProperty("style_1")]
        public string Style1 { get; set; }

        [JsonProperty("style_2")]
        public string Style2 { get; set; }

        [JsonProperty("wallpaper")]
        public string Wallpaper { get; set; }

        [JsonProperty("flooring")]
        public string Flooring { get; set; }

        [JsonProperty("furniture_List")]
        public string FurnitureList { get; set; }
    }
}
