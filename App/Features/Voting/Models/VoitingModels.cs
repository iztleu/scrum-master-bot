namespace App.Features.Voting.Models;

public record StartRequest(string TeamName, string VotingName);
public record VoteRequest(long VotingId, string Value);

public record GetActiveVotingRequest(string TeamName);

public record FinishVotingRequest(long VotingId);