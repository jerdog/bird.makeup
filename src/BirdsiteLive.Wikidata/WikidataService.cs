using System.Text.Json;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;

namespace BirdsiteLive.Wikidata;

public class WikidataService
{
    private ITwitterUserDal _dal;
    private readonly string _endpoint;
    private HttpClient _client = new ();

    private const string HandleQuery = """
                                       SELECT ?item ?handle ?fediHandle ?itemLabel ?itemDescription
                                       WHERE
                                       {
                                         ?item wdt:P2002 ?handle
                                          OPTIONAL {?item wdt:P4033 ?fediHandle} 
                                          SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
                                       } # LIMIT 10 
                                       """;
    private const string NotableWorkQuery = """
                                       SELECT ?item ?handle ?work
                                       WHERE
                                       {
                                         ?item wdt:P2002 ?handle .
                                         ?item wdt:P800 ?work
                                               SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
                                       } # LIMIT 100
                                       """;
    public WikidataService(ITwitterUserDal twitterUserDal)
    {
        _dal = twitterUserDal;

        string? key = Environment.GetEnvironmentVariable("semantic");
        if (key is null)
        {
            _endpoint = "https://query.wikidata.org/sparql?query=";
        }
        else
        {
            _endpoint = "https://query.semantic.builders/sparql?query=";
            _client.DefaultRequestHeaders.Add("api-key", key);   
        }
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("User-Agent", "BirdMakeup/1.0 (https://bird.makeup; https://sr.ht/~cloutier/bird.makeup/) BirdMakeup/1.0");
        _client.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task SyncQcodes()
    {
        
        var twitterUser = new HashSet<string>();
        var twitterUserQuery = await _dal.GetAllTwitterUsersAsync();
        Console.WriteLine("Loading twitter users");
        foreach (SyncTwitterUser user in twitterUserQuery)
        {
            twitterUser.Add(user.Acct);
        }
        Console.WriteLine($"Done loading {twitterUser.Count} twitter users");


        Console.WriteLine("Making Wikidata Query to " + _endpoint);
        var response = await _client.GetAsync(_endpoint + Uri.EscapeDataString(HandleQuery));
        var content = await response.Content.ReadAsStringAsync();
        var res = JsonDocument.Parse(content);
        Console.WriteLine("Done with Wikidata Query");


        foreach (JsonElement n in res.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            

            var qcode = n.GetProperty("item").GetProperty("value").GetString()!.Replace("http://www.wikidata.org/entity/", "");
            var acct = n.GetProperty("handle").GetProperty("value").GetString()!.ToLower().Trim().TrimEnd( '\r', '\n' );
            string? fediHandle = null;
            string? label = null;
            string? description = null;
            try
            {
                label = n.GetProperty("itemLabel").GetProperty("value").GetString();
                description = n.GetProperty("itemDescription").GetProperty("value").GetString();
                fediHandle = n.GetProperty("fediHandle").GetProperty("value").GetString();
            } catch (KeyNotFoundException _) {}


            if (twitterUser.Contains(acct))
            {
                Console.WriteLine($"{acct} with {qcode}");
                await _dal.UpdateUserExtradataAsync(acct, "wikidata", "qcode", qcode);
                if (fediHandle is not null)
                    await _dal.UpdateUserExtradataAsync(acct, "wikidata","fedihandle", fediHandle);
                if (label is not null)
                    await _dal.UpdateUserExtradataAsync(acct, "wikidata","label", label);
                if (description is not null)
                    await _dal.UpdateUserExtradataAsync(acct, "wikidata","description", description.Trim());
            }
        }
    }

    public async Task SyncNotableWork()
    {
        var twitterUser = new HashSet<string>();
        var twitterUserQuery = await _dal.GetAllTwitterUsersAsync();
        Console.WriteLine("Loading twitter users");
        foreach (SyncTwitterUser user in twitterUserQuery)
        {
            twitterUser.Add(user.Acct);
        }
        Console.WriteLine($"Done loading {twitterUser.Count} twitter users");
        
        
        Console.WriteLine("Making Wikidata Query to " + _endpoint);
        var response = await _client.GetAsync(_endpoint + Uri.EscapeDataString(NotableWorkQuery));
        var content = await response.Content.ReadAsStringAsync();
        var res = JsonDocument.Parse(content);
        Console.WriteLine("Done with Wikidata Query");

        var notableWork = new Dictionary<string, List<string>>();
        foreach (JsonElement n in res.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            var qcode =
                n.GetProperty("item").GetProperty("value").GetString()!.Replace("http://www.wikidata.org/entity/", "");
            var acct = n.GetProperty("handle").GetProperty("value").GetString()!.ToLower().Trim().TrimEnd('\r', '\n');
            var work =
                n.GetProperty("work").GetProperty("value").GetString()!.Replace("http://www.wikidata.org/entity/", "");

            List<string> workList;
            if (!notableWork.TryGetValue("qcode", out workList))
                workList = new List<string>();

            workList = workList.Append(work).ToList();

            notableWork[acct] = workList;
        }

        foreach ((string acct, List<string> works) in notableWork)
        {
            Console.WriteLine(acct + " " + works.Count);
            if (twitterUser.Contains(acct))
                await _dal.UpdateUserExtradataAsync(acct, "wikidata", "notableWorks", works);
        }

    }
}