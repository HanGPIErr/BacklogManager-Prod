using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class PokerService
    {
        private readonly IDatabase _database;
        private readonly BacklogService _backlogService;

        public PokerService(IDatabase database, BacklogService backlogService)
        {
            _database = database;
            _backlogService = backlogService;
        }

        public PokerSession CreatePokerSession(int backlogItemId)
        {
            var session = new PokerSession
            {
                BacklogItemId = backlogItemId,
                DateSession = DateTime.Now
            };
            return _database.AddOrUpdatePokerSession(session);
        }

        public void AddVote(int sessionId, int devId, int valeurVote)
        {
            var vote = new PokerVote
            {
                PokerSessionId = sessionId,
                DevId = devId,
                ValeurVote = valeurVote
            };
            _database.AddPokerVote(vote);
        }

        public List<PokerVote> GetVotesForSession(int sessionId)
        {
            return _database.GetPokerVotes().Where(x => x.PokerSessionId == sessionId).ToList();
        }

        public bool HasVoteGaps(int sessionId)
        {
            var votes = GetVotesForSession(sessionId);
            if (votes.Count < 2) return false;

            var values = votes.Select(v => v.ValeurVote).OrderBy(v => v).ToList();
            int min = values.First();
            int max = values.Last();

            return (max - min) > 1;
        }

        public int CalculateConsensus(int sessionId)
        {
            var votes = GetVotesForSession(sessionId);
            if (votes.Count == 0) return 0;

            // Calculate average and round
            double average = votes.Average(v => v.ValeurVote);
            return (int)Math.Round(average);
        }

        public void FinalizeSession(int sessionId, int consensus)
        {
            var session = _database.GetPokerSessions().FirstOrDefault(s => s.Id == sessionId);
            if (session == null) return;

            session.ComplexiteConsensus = consensus;
            session.JoursPlanifies = consensus * 1.25;
            _database.AddOrUpdatePokerSession(session);

            // Update the backlog item
            var backlogItem = _backlogService.GetBacklogItemById(session.BacklogItemId);
            if (backlogItem != null)
            {
                backlogItem.Complexite = consensus;
                _backlogService.SaveBacklogItem(backlogItem);
            }
        }

        public List<PokerSession> GetSessionsForBacklogItem(int backlogItemId)
        {
            return _database.GetPokerSessions().Where(s => s.BacklogItemId == backlogItemId).ToList();
        }
    }
}
