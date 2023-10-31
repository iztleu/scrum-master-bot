using MediatR;

namespace App.Features.Voting.Events;

public record VotingFinishEvent(long VotingId) : INotification;