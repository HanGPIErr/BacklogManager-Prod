using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class ProgrammeService
    {
        private readonly IDatabase _database;

        public ProgrammeService(IDatabase database)
        {
            _database = database;
        }

        public List<Programme> GetAllProgrammes()
        {
            return _database.GetAllProgrammes();
        }

        public Programme GetProgrammeById(int id)
        {
            return _database.GetProgrammeById(id);
        }

        public void AjouterProgramme(Programme programme)
        {
            if (string.IsNullOrWhiteSpace(programme.Nom))
            {
                throw new ArgumentException("Le nom du programme est obligatoire.");
            }

            programme.DateCreation = DateTime.Now;
            programme.Actif = true;

            _database.AjouterProgramme(programme);
        }

        public void ModifierProgramme(Programme programme)
        {
            if (string.IsNullOrWhiteSpace(programme.Nom))
            {
                throw new ArgumentException("Le nom du programme est obligatoire.");
            }

            _database.ModifierProgramme(programme);
        }

        public void SupprimerProgramme(int id)
        {
            _database.SupprimerProgramme(id);
        }

        public List<Projet> GetProjetsByProgramme(int programmeId)
        {
            return _database.GetProjetsByProgramme(programmeId);
        }

        // Statistiques
        public int GetNombreProjets(int programmeId)
        {
            return GetProjetsByProgramme(programmeId).Count;
        }

        public int GetNombreProjetsActifs(int programmeId)
        {
            return GetProjetsByProgramme(programmeId).Count(p => p.Actif);
        }
    }
}
