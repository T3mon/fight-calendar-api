using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FightCalendar.Sync.Firestore;

// Reads the "events" collection straight from Firestore's public REST API.
// No credentials needed: the tapology-firebase-scraper project's security
// rules allow anonymous reads (write requires anon auth, delete is blocked
// entirely) - see that repo's firestore.rules and README.
public class FirestoreEventsClient(HttpClient httpClient, IOptions<FirestoreOptions> options, ILogger<FirestoreEventsClient> logger)
{
    private readonly FirestoreOptions _options = options.Value;

    public async Task<List<TapologyEventDto>> FetchAllEventsAsync(CancellationToken ct = default)
    {
        var results = new List<TapologyEventDto>();
        string? pageToken = null;

        do
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_options.ProjectId}/databases/(default)/documents/{_options.CollectionName}?pageSize=300"
                       + (pageToken is null ? "" : $"&pageToken={Uri.EscapeDataString(pageToken)}");

            using var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("documents", out var documents))
            {
                foreach (var docElement in documents.EnumerateArray())
                {
                    try
                    {
                        results.Add(ParseEvent(docElement));
                    }
                    catch (Exception ex)
                    {
                        var docName = docElement.TryGetProperty("name", out var n) ? n.GetString() : "(unknown)";
                        logger.LogWarning(ex, "Skipping malformed Firestore document {DocName}", docName);
                    }
                }
            }

            pageToken = doc.RootElement.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null;
        } while (!string.IsNullOrEmpty(pageToken));

        return results;
    }

    private static TapologyEventDto ParseEvent(JsonElement docElement)
    {
        var name = docElement.GetProperty("name").GetString()!;
        var slug = name.Split('/').Last();
        var fields = docElement.GetProperty("fields");

        var organization = GetString(fields, "organization") ?? "Other";

        return new TapologyEventDto
        {
            Slug = slug,
            Title = GetString(fields, "title") ?? slug,
            Link = GetString(fields, "link") ?? "",
            Organization = organization,
            FullOrganization = GetString(fields, "fullOrganization") ?? organization,
            Date = GetString(fields, "date") ?? "",
            Venue = GetString(fields, "venue"),
            Location = GetString(fields, "location"),
            Fights = ParseFights(fields),
        };
    }

    private static List<TapologyFightDto> ParseFights(JsonElement fields)
    {
        var list = new List<TapologyFightDto>();

        if (!fields.TryGetProperty("fights", out var fightsField) ||
            !fightsField.TryGetProperty("arrayValue", out var arrayValue) ||
            !arrayValue.TryGetProperty("values", out var values))
        {
            return list;
        }

        foreach (var fightValue in values.EnumerateArray())
        {
            var fightFields = fightValue.GetProperty("mapValue").GetProperty("fields");

            var fighterA = ParseFighter(fightFields, "fighterA");
            var fighterB = ParseFighter(fightFields, "fighterB");
            if (fighterA is null || fighterB is null) continue;

            list.Add(new TapologyFightDto
            {
                FighterA = fighterA,
                FighterB = fighterB,
                WeightClass = GetString(fightFields, "weightClass"),
            });
        }

        return list;
    }

    private static TapologyFighterDto? ParseFighter(JsonElement fields, string key)
    {
        if (!fields.TryGetProperty(key, out var fighterValue) ||
            !fighterValue.TryGetProperty("mapValue", out var mapValue))
        {
            return null;
        }

        var fighterFields = mapValue.GetProperty("fields");
        var link = GetString(fighterFields, "link");
        var fighterName = GetString(fighterFields, "name");
        if (link is null || fighterName is null) return null;

        return new TapologyFighterDto
        {
            Name = fighterName,
            Record = GetString(fighterFields, "record"),
            Link = link,
        };
    }

    // Firestore's REST format wraps every value in a type tag, e.g.
    // {"stringValue": "foo"} or {"nullValue": null}. We only ever read
    // strings out of this feed, so anything else (including nullValue) is
    // treated as absent.
    private static string? GetString(JsonElement fields, string key)
    {
        if (!fields.TryGetProperty(key, out var value)) return null;
        return value.TryGetProperty("stringValue", out var str) ? str.GetString() : null;
    }
}
