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


public static class EventExtensions
{
    [GraphQLMember("entrants")]
    public static EntrantConnection Entrants(this Event  @event, [GraphQLArgument("query", "EventEntrantPageQuery")] EventEntrantPageQuery query = null)
    {
        return @event.GetMethodValue<EntrantConnection>("entrants", query);
    }

    [GraphQLMember("images")]
    public static List<Image> Images(this Event  @event, [GraphQLArgument("type", "String")] string type = null)
    {
        return @event.GetMethodValue<List<Image>>("images", type);
    }

    [GraphQLMember("phases")]
    public static List<Phase> Phases(this Event  @event, [GraphQLArgument("state", "ActivityState")] ActivityState? state = null, [GraphQLArgument("phaseId", "ID")] ID phaseId = null)
    {
        return @event.GetMethodValue<List<Phase>>("phases", state, phaseId);
    }

    [GraphQLMember("sets")]
    public static SetConnection Sets(this Event  @event, [GraphQLArgument("page", "Int")] int? page = null, [GraphQLArgument("perPage", "Int")] int? perPage = null, [GraphQLArgument("sortType", "SetSortType")] SetSortType? sortType = null, [GraphQLArgument("filters", "SetFilters")] SetFilters filters = null)
    {
        return @event.GetMethodValue<SetConnection>("sets", page, perPage, sortType, filters);
    }

    [GraphQLMember("standings")]
    public static StandingConnection Standings(this Event  @event, [GraphQLArgument("query", "StandingPaginationQuery!")] StandingPaginationQuery query)
    {
        return @event.GetMethodValue<StandingConnection>("standings", query);
    }

    [GraphQLMember("stations")]
    public static StationsConnection Stations(this Event  @event, [GraphQLArgument("query", "StationFilter")] StationFilter query = null)
    {
        return @event.GetMethodValue<StationsConnection>("stations", query);
    }

    [GraphQLMember("userEntrant")]
    public static Entrant UserEntrant(this Event  @event, [GraphQLArgument("userId", "ID")] ID userId = null)
    {
        return @event.GetMethodValue<Entrant>("userEntrant", userId);
    }

    [GraphQLMember("waves")]
    public static List<Wave> Waves(this Event  @event, [GraphQLArgument("phaseId", "ID")] ID phaseId = null)
    {
        return @event.GetMethodValue<List<Wave>>("waves", phaseId);
    }

}

/// <summary>
/// An event in a tournament
/// </summary>
public partial class Event : GraphQLTypeBase
{
    [GraphQLMember("id")]
    [JsonPropertyName("id")]
    public ID Id { get; set; }

    /// <summary>
    /// How long before the event start will the check-in end (in seconds)
    /// </summary>
    [GraphQLMember("checkInBuffer")]
    [JsonPropertyName("checkInBuffer")]
    public int? CheckInBuffer { get; set; }

    /// <summary>
    /// How long the event check-in will last (in seconds)
    /// </summary>
    [GraphQLMember("checkInDuration")]
    [JsonPropertyName("checkInDuration")]
    public int? CheckInDuration { get; set; }

    /// <summary>
    /// Whether check-in is enabled for this event
    /// </summary>
    [GraphQLMember("checkInEnabled")]
    [JsonPropertyName("checkInEnabled")]
    public bool? CheckInEnabled { get; set; }

    /// <summary>
    /// Rough categorization of event tier, denoting relative importance in the competitive scene
    /// </summary>
    [GraphQLMember("competitionTier")]
    [JsonPropertyName("competitionTier")]
    public int? CompetitionTier { get; set; }

    /// <summary>
    /// When the event was created (unix timestamp)
    /// </summary>
    [GraphQLMember("createdAt")]
    [JsonPropertyName("createdAt")]
    public Timestamp CreatedAt { get; set; }

    /// <summary>
    /// Last date attendees are able to create teams for team events
    /// </summary>
    [GraphQLMember("deckSubmissionDeadline")]
    [JsonPropertyName("deckSubmissionDeadline")]
    public Timestamp DeckSubmissionDeadline { get; set; }

    private LazyProperty<EntrantConnection> _entrants = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public EntrantConnection Entrants => _entrants.Value(() => GetFirstMethodValue<EntrantConnection>("entrants"));

    /// <summary>
    /// Whether the event has decks
    /// </summary>
    [GraphQLMember("hasDecks")]
    [JsonPropertyName("hasDecks")]
    public bool? HasDecks { get; set; }

    /// <summary>
    /// Are player tasks enabled for this event
    /// </summary>
    [GraphQLMember("hasTasks")]
    [JsonPropertyName("hasTasks")]
    public bool? HasTasks { get; set; }

    private LazyProperty<List<Image>> _images = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Image> Images => _images.Value(() => GetFirstMethodValue<List<Image>>("images"));

    /// <summary>
    /// Whether the event is an online event or not
    /// </summary>
    [GraphQLMember("isOnline")]
    [JsonPropertyName("isOnline")]
    public bool? IsOnline { get; set; }

    [GraphQLMember("league")]
    [JsonPropertyName("league")]
    public League League { get; set; }

    /// <summary>
    /// Markdown field for match rules/instructions
    /// </summary>
    [GraphQLMember("matchRulesMarkdown")]
    [JsonPropertyName("matchRulesMarkdown")]
    public string MatchRulesMarkdown { get; set; }

    /// <summary>
    /// Title of event set by organizer
    /// </summary>
    [GraphQLMember("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets the number of entrants in this event
    /// </summary>
    [GraphQLMember("numEntrants")]
    [JsonPropertyName("numEntrants")]
    public int? NumEntrants { get; set; }

    /// <summary>
    /// The phase groups that belong to an event.
    /// </summary>
    [GraphQLMember("phaseGroups")]
    [JsonPropertyName("phaseGroups")]
    public List<PhaseGroup> PhaseGroups { get; set; }

    private LazyProperty<List<Phase>> _phases = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Phase> Phases => _phases.Value(() => GetFirstMethodValue<List<Phase>>("phases"));

    /// <summary>
    /// TO settings for prizing
    /// </summary>
    [GraphQLMember("prizingInfo")]
    [JsonPropertyName("prizingInfo")]
    public JSON PrizingInfo { get; set; }

    [GraphQLMember("publishing")]
    [JsonPropertyName("publishing")]
    public JSON Publishing { get; set; }

    /// <summary>
    /// Markdown field for event rules/instructions
    /// </summary>
    [GraphQLMember("rulesMarkdown")]
    [JsonPropertyName("rulesMarkdown")]
    public string RulesMarkdown { get; set; }

    /// <summary>
    /// Id of the event ruleset
    /// </summary>
    [GraphQLMember("rulesetId")]
    [JsonPropertyName("rulesetId")]
    public int? RulesetId { get; set; }

    private LazyProperty<SetConnection> _sets = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public SetConnection Sets => _sets.Value(() => GetFirstMethodValue<SetConnection>("sets"));

    [GraphQLMember("slug")]
    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    private LazyProperty<StandingConnection> _standings = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public StandingConnection Standings => _standings.Value(() => GetFirstMethodValue<StandingConnection>("standings"));

    /// <summary>
    /// When does this event start?
    /// </summary>
    [GraphQLMember("startAt")]
    [JsonPropertyName("startAt")]
    public Timestamp StartAt { get; set; }

    /// <summary>
    /// The state of the Event.
    /// </summary>
    [GraphQLMember("state")]
    [JsonPropertyName("state")]
    public ActivityState? State { get; set; }

    private LazyProperty<StationsConnection> _stations = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public StationsConnection Stations => _stations.Value(() => GetFirstMethodValue<StationsConnection>("stations"));

    /// <summary>
    /// Last date attendees are able to create teams for team events
    /// </summary>
    [GraphQLMember("teamManagementDeadline")]
    [JsonPropertyName("teamManagementDeadline")]
    public Timestamp TeamManagementDeadline { get; set; }

    /// <summary>
    /// If this is a teams event, returns whether or not teams can set custom names
    /// </summary>
    [GraphQLMember("teamNameAllowed")]
    [JsonPropertyName("teamNameAllowed")]
    public bool? TeamNameAllowed { get; set; }

    /// <summary>
    /// Team roster size requirements
    /// </summary>
    [GraphQLMember("teamRosterSize")]
    [JsonPropertyName("teamRosterSize")]
    public TeamRosterSize TeamRosterSize { get; set; }

    [GraphQLMember("tournament")]
    [JsonPropertyName("tournament")]
    public Tournament Tournament { get; set; }

    /// <summary>
    /// The type of the event, whether an entrant will have one participant or multiple
    /// </summary>
    [GraphQLMember("type")]
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// When the event was last modified (unix timestamp)
    /// </summary>
    [GraphQLMember("updatedAt")]
    [JsonPropertyName("updatedAt")]
    public Timestamp UpdatedAt { get; set; }

    /// <summary>
    /// Whether the event uses the new EventSeeds for seeding
    /// </summary>
    [GraphQLMember("useEventSeeds")]
    [JsonPropertyName("useEventSeeds")]
    public bool? UseEventSeeds { get; set; }

    private LazyProperty<Entrant> _userEntrant = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public Entrant UserEntrant => _userEntrant.Value(() => GetFirstMethodValue<Entrant>("userEntrant"));

    [GraphQLMember("videogame")]
    [JsonPropertyName("videogame")]
    public Videogame Videogame { get; set; }

    private LazyProperty<List<Wave>> _waves = new();
    /// <summary>
    /// Do not use in Query, only to retrive result
    /// </summary>
    public List<Wave> Waves => _waves.Value(() => GetFirstMethodValue<List<Wave>>("waves"));

}
