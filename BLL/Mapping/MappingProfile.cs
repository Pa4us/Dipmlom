using AutoMapper;
using DAL.Entities;
using SharedModel.DTOs;

namespace BLL.Mapping
{
    public class MappingProfile: Profile
    {
        public MappingProfile() 
        {
            CreateMap<Role, RoleDto>();
            CreateMap<CreateRoleDto, Role>();
            CreateMap<UpdateRoleDto, Role>();

            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name));
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
            CreateMap<UpdateUserDto, User>();

            // Block mappings
            CreateMap<Block, BlockDto>()
                .ForMember(dest => dest.RoomCount, opt => opt.MapFrom(src => src.Rooms.Count));
            CreateMap<CreateBlockDto, Block>();
            CreateMap<UpdateBlockDto, Block>();

            // Room mappings
            CreateMap<Room, RoomDto>()
                .ForMember(dest => dest.BlockNumber, opt => opt.MapFrom(src => src.Block.BlockNumber));
            CreateMap<CreateRoomDto, Room>();
            CreateMap<UpdateRoomDto, Room>();

            // RepairRequest mappings
            CreateMap<RepairRequest, RepairRequestDto>()
                .ForMember(dest => dest.BlockNumber, opt => opt.MapFrom(src => src.Block.BlockNumber))
                .ForMember(dest => dest.RoomNumber, opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : null))
                .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src => src.RequestedBy.FullName))
                .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src => src.AssignedTo != null ? src.AssignedTo.FullName : null));
            CreateMap<CreateRepairRequestDto, RepairRequest>();
            CreateMap<UpdateRepairRequestDto, RepairRequest>();

            // Inspection mappings
            CreateMap<Inspection, InspectionDto>()
                .ForMember(dest => dest.BlockNumber,   opt => opt.MapFrom(src => src.Block.BlockNumber))
                .ForMember(dest => dest.Floor,         opt => opt.MapFrom(src => src.Block.Floor))
                .ForMember(dest => dest.RoomNumber,    opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : null))
                .ForMember(dest => dest.ZoneName,      opt => opt.MapFrom(src => src.Zone.DisplayName))
                .ForMember(dest => dest.InspectorName, opt => opt.MapFrom(src => src.Inspector.FullName));
            CreateMap<CreateInspectionDto, Inspection>();

            // Residence mappings
            CreateMap<Residence, ResidenceDto>()
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.RoomNumber, opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : null))
                .ForMember(dest => dest.BlockNumber, opt => opt.MapFrom(src => src.Block != null ? src.Block.BlockNumber : null))
                .ForMember(dest => dest.Floor, opt => opt.MapFrom(src => src.Block != null ? src.Block.Floor : 0));
            CreateMap<CreateResidenceDto, Residence>()
                .ForMember(dest => dest.IsCurrent, opt => opt.MapFrom(src => true));
            CreateMap<UpdateResidenceDto, Residence>();

            // StudentPoint mappings
            CreateMap<StudentPoint, StudentPointDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null));
            CreateMap<CreateStudentPointDto, StudentPoint>();
            CreateMap<UpdateStudentPointDto, StudentPoint>();

            // RepairComment mappings
            CreateMap<RepairComment, RepairCommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Неизвестный"));
            CreateMap<RepairCommentDto, RepairComment>();

            // BlockWeeklyScore mappings
            CreateMap<BlockWeeklyScore, BlockWeeklyScoreDto>()
                .ForMember(dest => dest.BlockNumber, opt => opt.MapFrom(src => src.Block != null ? src.Block.BlockNumber : string.Empty));

            // Event mappings
            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.Organizer.FullName))
                .ForMember(dest => dest.ParticipantsCount, opt => opt.MapFrom(src => src.EventParticipants.Count));
            CreateMap<CreateEventDto, Event>();
            CreateMap<UpdateEventDto, Event>();

            // UpdateInspection mapping
            CreateMap<UpdateInspectionDto, Inspection>()
                .ForMember(dest => dest.PhotoPath, opt => opt.Ignore());
        }
    }
}
