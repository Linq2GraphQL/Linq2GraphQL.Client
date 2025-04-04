//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Common;

namespace StartGG.Client;


public static class LeagueExtensions
{
    [GraphQLMember("eventOwners")]
    public static EventOwnerConnection EventOwners(this League  league, [GraphQLArgument("query", "EventOwnersQuery")] EventOwnersQuery query = null)
    {
        return league.GetMethodValue<EventOwnerConnection>("eventOwners", query);
    }

    [GraphQLMember("events")]
    public static EventConnection Events(this League  league, [GraphQLArgument("query", "LeagueEventsQuery")] LeagueEventsQuery query = null)
    {
        return league.GetMethodValue<EventConnection>("events", query);
    }

    [GraphQLMember("images")]
    public static List<Image> Images(this League  league, [GraphQLArgument("type", "String")] string type = null)
    {
        return league.GetMethodValue<List<Image>>("images", type);
    }

    [GraphQLMember("standings")]
    public static StandingConnection Standings(this League  league, [GraphQLArgument("query", "StandingGroupStandingPageFilter")] StandingGroupStandingPageFilter query = null)
    {
        return league.GetMethodValue<StandingConnection>("standings", query);
    }

    [GraphQLMember("url")]
    public static string Url(this League  league, [GraphQLArgument("tab", "String")] string tab = null, [GraphQLArgument("relative", "Boolean")] bool? relative = null)
    {
        return league.GetMethodValue<string>("url", tab, relative);
    }

}

/// <summary>
/// A league
/// </summary>
public partial class League : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    [GraphQLMember("addrState")]
    [JsonPropertyName("addrState")]
    public string AddrState { get; set; }

    [GraphQLMember("city")]
    [JsonPropertyName("city")]
    public string City { get; set; }

    [GraphQLMember("countryCode")]
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; }

    /// <summary>
    /// When the tournament was created (unix timestamp)
    /// </summary>
    [GraphQLMember("createdAt")]
    [JsonPropertyName("createdAt")]
    public Timestamp CreatedAt { get; set; }

    [GraphQLMember("currency")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// When the tournament ends
    /// </summary>
    [GraphQLMember("endAt")]
    [JsonPropertyName("endAt")]
    public Timestamp EndAt { get; set; }

    [GraphQLMember("entrantCount")]
    [JsonPropertyName("entrantCount")]
    public int? EntrantCount { get; set; }

    private LazyProperty<EventOwnerConnection> _eventOwners = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public EventOwnerConnection EventOwners => _eventOwners.Value(() => GetFirstMethodValue<EventOwnerConnection>("eventOwners"));

    /// <summary>
    /// When does event registration close
    /// </summary>
    [GraphQLMember("eventRegistrationClosesAt")]
    [JsonPropertyName("eventRegistrationClosesAt")]
    public Timestamp EventRegistrationClosesAt { get; set; }

    private LazyProperty<EventConnection> _events = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public EventConnection Events => _events.Value(() => GetFirstMethodValue<EventConnection>("events"));

    /// <summary>
    /// True if tournament has at least one offline event
    /// </summary>
    [GraphQLMember("hasOfflineEvents")]
    [JsonPropertyName("hasOfflineEvents")]
    public bool? HasOfflineEvents { get; set; }

    [GraphQLMember("hasOnlineEvents")]
    [JsonPropertyName("hasOnlineEvents")]
    public bool? HasOnlineEvents { get; set; }

    [GraphQLMember("hashtag")]
    [JsonPropertyName("hashtag")]
    public string Hashtag { get; set; }

    private LazyProperty<List<Image>> _images = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Image> Images => _images.Value(() => GetFirstMethodValue<List<Image>>("images"));

    /// <summary>
    /// True if tournament has at least one online event
    /// </summary>
    [GraphQLMember("isOnline")]
    [JsonPropertyName("isOnline")]
    public bool? IsOnline { get; set; }

    [GraphQLMember("lat")]
    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [GraphQLMember("links")]
    [JsonPropertyName("links")]
    public TournamentLinks Links { get; set; }

    [GraphQLMember("lng")]
    [JsonPropertyName("lng")]
    public double? Lng { get; set; }

    [GraphQLMember("mapsPlaceId")]
    [JsonPropertyName("mapsPlaceId")]
    public string MapsPlaceId { get; set; }

    /// <summary>
    /// The tournament name
    /// </summary>
    [GraphQLMember("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [GraphQLMember("numUniquePlayers")]
    [JsonPropertyName("numUniquePlayers")]
    public int? NumUniquePlayers { get; set; }

    [GraphQLMember("postalCode")]
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; }

    [GraphQLMember("primaryContact")]
    [JsonPropertyName("primaryContact")]
    public string PrimaryContact { get; set; }

    [GraphQLMember("primaryContactType")]
    [JsonPropertyName("primaryContactType")]
    public string PrimaryContactType { get; set; }

    /// <summary>
    /// Publishing settings for this tournament
    /// </summary>
    [GraphQLMember("publishing")]
    [JsonPropertyName("publishing")]
    public JSON Publishing { get; set; }

    /// <summary>
    /// When does registration for the tournament end
    /// </summary>
    [GraphQLMember("registrationClosesAt")]
    [JsonPropertyName("registrationClosesAt")]
    public Timestamp RegistrationClosesAt { get; set; }

    [GraphQLMember("rules")]
    [JsonPropertyName("rules")]
    public string Rules { get; set; }

    /// <summary>
    /// The short slug used to form the url
    /// </summary>
    [GraphQLMember("shortSlug")]
    [JsonPropertyName("shortSlug")]
    public string ShortSlug { get; set; }

    /// <summary>
    /// Whether standings for this league should be visible
    /// </summary>
    [GraphQLMember("showStandings")]
    [JsonPropertyName("showStandings")]
    public bool? ShowStandings { get; set; }

    [GraphQLMember("slug")]
    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    private LazyProperty<StandingConnection> _standings = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public StandingConnection Standings => _standings.Value(() => GetFirstMethodValue<StandingConnection>("standings"));

    /// <summary>
    /// When the tournament Starts
    /// </summary>
    [GraphQLMember("startAt")]
    [JsonPropertyName("startAt")]
    public Timestamp StartAt { get; set; }

    /// <summary>
    /// State of the tournament, can be ActivityState::CREATED, ActivityState::ACTIVE, or ActivityState::COMPLETED
    /// </summary>
    [GraphQLMember("state")]
    [JsonPropertyName("state")]
    public int? State { get; set; }

    /// <summary>
    /// When is the team creation deadline
    /// </summary>
    [GraphQLMember("teamCreationClosesAt")]
    [JsonPropertyName("teamCreationClosesAt")]
    public Timestamp TeamCreationClosesAt { get; set; }

    [GraphQLMember("tiers")]
    [JsonPropertyName("tiers")]
    public List<EventTier> Tiers { get; set; }

    /// <summary>
    /// The timezone of the tournament
    /// </summary>
    [GraphQLMember("timezone")]
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; }

    /// <summary>
    /// The type of tournament from TournamentType
    /// </summary>
    [GraphQLMember("tournamentType")]
    [JsonPropertyName("tournamentType")]
    public int? TournamentType { get; set; }

    /// <summary>
    /// When the tournament was last modified (unix timestamp)
    /// </summary>
    [GraphQLMember("updatedAt")]
    [JsonPropertyName("updatedAt")]
    public Timestamp UpdatedAt { get; set; }

    private LazyProperty<string> _url = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public string Url => _url.Value(() => GetFirstMethodValue<string>("url"));

    [GraphQLMember("venueAddress")]
    [JsonPropertyName("venueAddress")]
    public string VenueAddress { get; set; }

    [GraphQLMember("venueName")]
    [JsonPropertyName("venueName")]
    public string VenueName { get; set; }

    [GraphQLMember("videogames")]
    [JsonPropertyName("videogames")]
    public List<Videogame> Videogames { get; set; }

}
