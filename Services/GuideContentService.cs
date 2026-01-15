using System.Collections.Generic;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class GuideContentService
    {
        private readonly LocalizationService _localizationService;

        public GuideContentService()
        {
            _localizationService = LocalizationService.Instance;
        }

        public Dictionary<string, string> GetQuestionsForRole(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Administrateur:
                    return GetAdministrateurQuestions();
                case RoleType.ChefDeProjet:
                    return GetChefDeProjetQuestions();
                case RoleType.Developpeur:
                    return GetDeveloppeurQuestions();
                case RoleType.BusinessAnalyst:
                    return GetBusinessAnalystQuestions();
                default:
                    return GetGeneralQuestions();
            }
        }

        private Dictionary<string, string> GetAdministrateurQuestions()
        {
            return new Dictionary<string, string>
            {
                { _localizationService["Guide_Admin_Q1"], _localizationService["Guide_Admin_A1"] },
                { _localizationService["Guide_Admin_Q2"], _localizationService["Guide_Admin_A2"] },
                { _localizationService["Guide_Admin_Q3"], _localizationService["Guide_Admin_A3"] },
                { _localizationService["Guide_Admin_Q4"], _localizationService["Guide_Admin_A4"] },
                { _localizationService["Guide_Admin_Q5"], _localizationService["Guide_Admin_A5"] },
                { _localizationService["Guide_Admin_Q6"], _localizationService["Guide_Admin_A6"] },
                { _localizationService["Guide_Admin_Q7"], _localizationService["Guide_Admin_A7"] }
            };
        }

        private Dictionary<string, string> GetChefDeProjetQuestions()
        {
            return new Dictionary<string, string>
            {
                { _localizationService["Guide_CP_Q1"], _localizationService["Guide_CP_A1"] },
                { _localizationService["Guide_CP_Q2"], _localizationService["Guide_CP_A2"] },
                { _localizationService["Guide_CP_Q3"], _localizationService["Guide_CP_A3"] },
                { _localizationService["Guide_CP_Q4"], _localizationService["Guide_CP_A4"] },
                { _localizationService["Guide_CP_Q5"], _localizationService["Guide_CP_A5"] },
                { _localizationService["Guide_CP_Q6"], _localizationService["Guide_CP_A6"] },
                { _localizationService["Guide_CP_Q7"], _localizationService["Guide_CP_A7"] }
            };
        }

        private Dictionary<string, string> GetDeveloppeurQuestions()
        {
            return new Dictionary<string, string>
            {
                { _localizationService["Guide_Dev_Q1"], _localizationService["Guide_Dev_A1"] },
                { _localizationService["Guide_Dev_Q2"], _localizationService["Guide_Dev_A2"] },
                { _localizationService["Guide_Dev_Q3"], _localizationService["Guide_Dev_A3"] },
                { _localizationService["Guide_Dev_Q4"], _localizationService["Guide_Dev_A4"] },
                { _localizationService["Guide_Dev_Q5"], _localizationService["Guide_Dev_A5"] },
                { _localizationService["Guide_Dev_Q6"], _localizationService["Guide_Dev_A6"] },
                { _localizationService["Guide_Dev_Q7"], _localizationService["Guide_Dev_A7"] }
            };
        }

        private Dictionary<string, string> GetBusinessAnalystQuestions()
        {
            return new Dictionary<string, string>
            {
                { _localizationService["Guide_BA_Q1"], _localizationService["Guide_BA_A1"] },
                { _localizationService["Guide_BA_Q2"], _localizationService["Guide_BA_A2"] },
                { _localizationService["Guide_BA_Q3"], _localizationService["Guide_BA_A3"] },
                { _localizationService["Guide_BA_Q4"], _localizationService["Guide_BA_A4"] },
                { _localizationService["Guide_BA_Q5"], _localizationService["Guide_BA_A5"] },
                { _localizationService["Guide_BA_Q6"], _localizationService["Guide_BA_A6"] }
            };
        }

        private Dictionary<string, string> GetGeneralQuestions()
        {
            return new Dictionary<string, string>
            {
                { _localizationService["Guide_General_Q1"], _localizationService["Guide_General_A1"] }
            };
        }
    }
}
