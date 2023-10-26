using System.Text.Json;
using System.Text.Json.Serialization;

namespace TasksTracker.TasksManager.Backend.Api.Services
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _dateFormatString;

        public DateTimeConverter(string dateFormatString)
        {
            _dateFormatString = dateFormatString;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();

            if (dateString != null) {
                return DateTime.ParseExact(dateString, _dateFormatString, System.Globalization.CultureInfo.InvariantCulture);
            } else {
                throw new("Date string from reader is null.");
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_dateFormatString));
        }
    }
}