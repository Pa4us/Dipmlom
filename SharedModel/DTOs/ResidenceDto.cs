using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModel.DTOs
{
    public class ResidenceDto : BaseDto
    {
        public int UserId { get; set; }                     // ID студента
        public string? UserFullName { get; set; }           // ФИО студента
        public string? Username { get; set; }               // Логин студента
        public int RoomId { get; set; }                     // ID комнаты
        public string? RoomNumber { get; set; }             // Номер комнаты
        public int BlockId { get; set; }                    // ID блока
        public string? BlockNumber { get; set; }            // Номер блока (формат "44-1")
        public int Floor { get; set; }                      // Этаж
        public DateOnly MoveInDate { get; set; }            // Дата заселения
        public DateOnly? MoveOutDate { get; set; }          // Дата выселения (null - проживает до сих пор)
        public bool IsCurrent { get; set; }                 // Является ли текущим проживанием

        public int DurationDays                             // Длительность проживания в днях
        {
            get
            {
                var endDate = MoveOutDate ?? DateOnly.FromDateTime(DateTime.Today);
                return endDate.DayNumber - MoveInDate.DayNumber;
            }
        }
    }

    public class CreateResidenceDto
    {
        public int UserId { get; set; }                     // ID студента
        public int RoomId { get; set; }                     // ID комнаты
        public DateOnly MoveInDate { get; set; }            // Дата заселения (по умолчанию - сегодня)
    }

    public class UpdateResidenceDto
    {
        public int Id { get; set; }                         // ID записи о проживании
        public DateOnly MoveOutDate { get; set; }           // Дата выселения (по умолчанию - сегодня)
        public bool IsCurrent { get; set; }                 // Является ли текущим проживанием (при выселении = false)
    }

    public class RelocateResidenceDto
    {
        public int CurrentResidenceId { get; set; }         // ID текущей записи о проживании
        public int NewRoomId { get; set; }                  // ID новой комнаты
        public DateOnly RelocateDate { get; set; }          // Дата переезда (по умолчанию - сегодня)
    }

    public class ResidenceAvailabilityCheckDto
    {
        public int RoomId { get; set; }                     // ID комнаты
        public string? RoomNumber { get; set; }             // Номер комнаты
        public int BlockId { get; set; }                    // ID блока
        public string? BlockNumber { get; set; }            // Номер блока
        public int Capacity { get; set; }                   // Вместимость комнаты
        public int CurrentOccupancy { get; set; }           // Текущее количество проживающих
        public bool HasFreeSpace => Capacity - CurrentOccupancy > 0;  // Свободно ли место
        public int FreeSpaces => Capacity - CurrentOccupancy;        // Количество свободных мест
    }
}
