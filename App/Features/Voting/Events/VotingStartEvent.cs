using MediatR;

namespace App.Features.Voting.Events;

public record VotingStartEvent(long VotingId) : INotification;