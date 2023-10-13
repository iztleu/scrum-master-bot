using MediatR;

namespace App.Features.Voting.Events;

public record VotingAutoFinishEvent(long VotingId) : INotification;