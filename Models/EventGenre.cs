using System.ComponentModel.DataAnnotations;

namespace EventsApp.Models
{
    public enum EventGenre
    {
        [Display(Name = "Друго")]
        Other = 0,
        [Display(Name = "Рок")]
        Rock = 1,
        [Display(Name = "Поп")]
        Pop = 2,
        [Display(Name = "Хип-хоп")]
        HipHop = 3,
        [Display(Name = "Електронна")]
        Electronic = 4,
        [Display(Name = "Джаз")]
        Jazz = 5,
        [Display(Name = "Класическа")]
        Classical = 6,
        [Display(Name = "Фолк")]
        Folk = 7,
        [Display(Name = "Метъл")]
        Metal = 8,
        [Display(Name = "Театър")]
        Theater = 9,
        [Display(Name = "Стендъп")]
        Standup = 10,
        [Display(Name = "Фестивал")]
        Festival = 11,
        [Display(Name = "Изложба")]
        Exhibition = 12,
        [Display(Name = "Спорт")]
        Sports = 13,
        [Display(Name = "Конференция")]
        Conference = 14,
        [Display(Name = "Работилница")]
        Workshop = 15,
    }
}
