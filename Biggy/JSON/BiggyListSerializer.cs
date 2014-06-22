using Newtonsoft.Json;
using System.Collections;

namespace Biggy.JSON
{
  public class BiggyListSerializer : JsonConverter
  {

    public override bool CanConvert(System.Type objectType)
    {
      throw new System.NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new System.NotImplementedException();
    }

    // Custom Biggylist serialization which simply writes each object, separated by newlines, to the output
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      var list = value as IEnumerable;

      // Loop over all items in the list
      foreach (var item in list)
      {
        // Serialize the object to the writer
        serializer.Serialize(writer, item);

        // Separate with newline characters
        writer.WriteRaw("\r\n");
      }
    }
  }
}
