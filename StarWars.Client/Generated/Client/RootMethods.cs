using System.Collections.Generic;
using System;
using Linq2GraphQL.Client;

namespace StarWars.Client;

public class RootMethods
{
    private readonly GraphClient client;

    public RootMethods(GraphClient client)
    {
        this.client = client;
    }

    public GraphCursorQuery<FilmsConnection> AllFilms(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<FilmsConnection>(client,  "allFilms", OperationType.Query, arguments); 
    }

    public GraphQuery<Film> Film(string id = null, string filmID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("filmID","ID", filmID),
        };

        return new GraphQuery<Film>(client,  "film", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<PeopleConnection> AllPeople(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<PeopleConnection>(client,  "allPeople", OperationType.Query, arguments); 
    }

    public GraphQuery<Person> Person(string id = null, string personID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("personID","ID", personID),
        };

        return new GraphQuery<Person>(client,  "person", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<PlanetsConnection> AllPlanets(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<PlanetsConnection>(client,  "allPlanets", OperationType.Query, arguments); 
    }

    public GraphQuery<Planet> Planet(string id = null, string planetID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("planetID","ID", planetID),
        };

        return new GraphQuery<Planet>(client,  "planet", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<SpeciesConnection> AllSpecies(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<SpeciesConnection>(client,  "allSpecies", OperationType.Query, arguments); 
    }

    public GraphQuery<Species> Species(string id = null, string speciesID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("speciesID","ID", speciesID),
        };

        return new GraphQuery<Species>(client,  "species", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<StarshipsConnection> AllStarships(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<StarshipsConnection>(client,  "allStarships", OperationType.Query, arguments); 
    }

    public GraphQuery<Starship> Starship(string id = null, string starshipID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("starshipID","ID", starshipID),
        };

        return new GraphQuery<Starship>(client,  "starship", OperationType.Query, arguments); 
    }

    public GraphCursorQuery<VehiclesConnection> AllVehicles(string after = null, int? first = null, string before = null, int? last = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("after","String", after),
    	    new("first","Int", first),
    	    new("before","String", before),
    	    new("last","Int", last),
        };

        return new GraphCursorQuery<VehiclesConnection>(client,  "allVehicles", OperationType.Query, arguments); 
    }

    public GraphQuery<Vehicle> Vehicle(string id = null, string vehicleID = null)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID", id),
    	    new("vehicleID","ID", vehicleID),
        };

        return new GraphQuery<Vehicle>(client,  "vehicle", OperationType.Query, arguments); 
    }

    public GraphQuery<Node> Node(string id)
    {
	    var arguments = new List<ArgumentValue>
        {
    	    new("id","ID!", id),
        };

        return new GraphQuery<Node>(client,  "node", OperationType.Query, arguments); 
    }

    }
