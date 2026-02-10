using AutoMapper;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;

namespace Quantix.Service.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<AppUser, UserDto>();
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
        CreateMap<CreateTransactionDto, Transaction>();
    }
}
