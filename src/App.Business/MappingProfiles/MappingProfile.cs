using App.Business.DTOs.Attendance;
using App.Business.DTOs.Auth;
using App.Business.DTOs.Cashboxes;
using App.Business.DTOs.Children;
using App.Business.DTOs.Divisions;
using App.Business.DTOs.Groups;
using App.Business.DTOs.Payments;
using App.Business.DTOs.Schedule;
using App.Core.Entities;
using App.Core.Entities.Identity;
using AutoMapper;

namespace App.Business.MappingProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User
            CreateMap<User, UserResponse>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));

            // Child
            CreateMap<CreateChildRequest, Child>();
            CreateMap<Child, ChildResponse>()
                .ForMember(d => d.GroupName, opt => opt.MapFrom(s => s.Group != null ? s.Group.Name : string.Empty))
                .ForMember(d => d.DivisionName, opt => opt.MapFrom(s => s.Group != null && s.Group.Division != null ? s.Group.Division.Name : string.Empty))
                .ForMember(d => d.ScheduleType, opt => opt.MapFrom(s => s.ScheduleType.ToString()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
            CreateMap<Child, ChildDetailResponse>()
                .ForMember(d => d.GroupName, opt => opt.MapFrom(s => s.Group != null ? s.Group.Name : string.Empty))
                .ForMember(d => d.DivisionName, opt => opt.MapFrom(s => s.Group != null && s.Group.Division != null ? s.Group.Division.Name : string.Empty))
                .ForMember(d => d.ScheduleType, opt => opt.MapFrom(s => s.ScheduleType.ToString()))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.TeacherName, opt => opt.MapFrom(s => s.Group != null && s.Group.Teacher != null ? $"{s.Group.Teacher.FirstName} {s.Group.Teacher.LastName}" : string.Empty));

            // Group
            CreateMap<CreateGroupRequest, Group>();
            CreateMap<Group, GroupResponse>()
                .ForMember(d => d.DivisionName, opt => opt.MapFrom(s => s.Division != null ? s.Division.Name : string.Empty))
                .ForMember(d => d.TeacherName, opt => opt.MapFrom(s => s.Teacher != null ? $"{s.Teacher.FirstName} {s.Teacher.LastName}" : string.Empty))
                .ForMember(d => d.CurrentChildCount, opt => opt.MapFrom(s => s.Children != null ? s.Children.Count : 0));
            CreateMap<Group, GroupDetailResponse>()
                .ForMember(d => d.DivisionName, opt => opt.MapFrom(s => s.Division != null ? s.Division.Name : string.Empty))
                .ForMember(d => d.TeacherName, opt => opt.MapFrom(s => s.Teacher != null ? $"{s.Teacher.FirstName} {s.Teacher.LastName}" : string.Empty))
                .ForMember(d => d.CurrentChildCount, opt => opt.MapFrom(s => s.Children != null ? s.Children.Count : 0))
                .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children));
            CreateMap<Child, GroupChildItem>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.ScheduleType, opt => opt.MapFrom(s => s.ScheduleType.ToString()));

            // Division
            CreateMap<CreateDivisionRequest, Division>();
            CreateMap<Division, DivisionResponse>()
                .ForMember(d => d.GroupCount, opt => opt.MapFrom(s => s.Groups != null ? s.Groups.Count : 0));

            // Attendance
            CreateMap<MarkAttendanceRequest, Attendance>();
            CreateMap<Attendance, AttendanceResponse>()
                .ForMember(d => d.ChildFullName, opt => opt.MapFrom(s => s.Child != null ? $"{s.Child.FirstName} {s.Child.LastName}" : string.Empty))
                .ForMember(d => d.GroupName, opt => opt.MapFrom(s => s.Child != null && s.Child.Group != null ? s.Child.Group.Name : string.Empty));

            // Payment
            CreateMap<Payment, PaymentResponse>()
                .ForMember(d => d.ChildFullName, opt => opt.MapFrom(s => s.Child != null ? $"{s.Child.FirstName} {s.Child.LastName}" : string.Empty))
                .ForMember(d => d.GroupName, opt => opt.MapFrom(s => s.Child != null && s.Child.Group != null ? s.Child.Group.Name : string.Empty))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.DiscountType, opt => opt.MapFrom(s => s.DiscountType.ToString()))
                .ForMember(d => d.CashboxName, opt => opt.MapFrom(s => s.Cashbox != null ? s.Cashbox.Name : null))
                .ForMember(d => d.CashboxType, opt => opt.MapFrom(s => s.Cashbox != null ? s.Cashbox.Type.ToString() : null))
                .ForMember(d => d.RemainingDebt, opt => opt.MapFrom(s => s.FinalAmount - s.PaidAmount));

            // Cashbox
            CreateMap<Cashbox, CashboxResponse>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));

            // Schedule
            CreateMap<ScheduleConfig, ScheduleConfigResponse>()
                .ForMember(d => d.ScheduleType, opt => opt.MapFrom(s => s.ScheduleType.ToString()));
        }
    }
}
