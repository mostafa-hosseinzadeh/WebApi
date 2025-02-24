﻿using AutoMapper;
using Common;
using Data.Contracts;
using DataTransferObjects.DTOs.Shared;
using DataTransferObjects.DTOs.Shared.Users;
using DataTransferObjects.DTOs.V1.Persons;
using Entities.PersonModels;
using Microsoft.EntityFrameworkCore;
using Services.ServicesContracts.V2.Users;
using ServicesContracts.V2.Users;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Services.V2.Users
{
    public class ForgetPasswordService : IForgetPasswordService
    {
        private readonly IRepository<CrePerson> _PersonRepository;
        private readonly IUsersService _UserService;
        private readonly IRepository<HrForgetPassword> _Repository;
        private readonly IMapper _Mapper;
        private const int AllowedNumberRequest = 3;
        private const int AllowedRemainingTime = -15;//minutes
        private const int AllowedRequestTime = -2;//minutes

        public ForgetPasswordService(IRepository<CrePerson> PersonRepository, IRepository<HrForgetPassword> Repository
            , IUsersService userService, IMapper mapper)
        {
            _PersonRepository = PersonRepository;
            _Repository = Repository;
            _UserService = userService;
            _Mapper = mapper;
        }

        public async Task<ApiResult<PersonBriefListDto>> ChangePassword(ForgetPasswordCuDto dto, CancellationToken cancellationToken)
        {
            var resultUser = await _UserService.GetUsersByNationalId(new ApplicationUserSearchDto() { NationalId = dto.NationalCode }, cancellationToken);

            if (!resultUser.IsSuccess)
                return new ApiResult<PersonBriefListDto>(false, resultUser.StatusCode, null);

            var user = resultUser.Data.FirstOrDefault();

            var lastRequestCode = await _Repository.TableNoTracking.Where(x => x.FPersonsId == user.PersonId)
                .OrderByDescending(x => x.IssueDate).FirstOrDefaultAsync();

            if (lastRequestCode.Code != dto.Code || lastRequestCode.IssueDate < DateTime.Now.AddMinutes(AllowedRemainingTime))
                return new ApiResult<PersonBriefListDto>(false, ApiResultStatusCode.ForgetPasswordCodeIsNotValid, null);

            var changePasswordTo = new ApplicationUserSearchDto.ApplicationUserChangePasswordDto()
            {
                CurrentPassword = dto.CurrentPassword,
                NewPassword = dto.NewPassword,
                UserName = user.UserName
            };

            var result = await _UserService.ChangePassword(changePasswordTo, false, cancellationToken);

            if (result.IsSuccess)
            {
                var requests = await _Repository.TableNoTracking.Where(x => x.FPersonsId == user.PersonId).ToListAsync();

                foreach (var item in requests)
                {
                    item.IsActive = false;
                    await _Repository.UpdateAsync(item, cancellationToken);
                }

                return new ApiResult<PersonBriefListDto>(true, ApiResultStatusCode.Success, null);
            }
            else
                return new ApiResult<PersonBriefListDto>(true, result.StatusCode, null);
        }

        //public async Task<ApiResult<ForgetPasswordListDto>> GenerateCode(string nationalCode, CancellationToken cancellationToken)
        //{
        //    var exist = _PersonRepository.TableNoTracking.Where(x => x.IndividualPerson.NationalId == nationalCode)
        //        .ProjectTo<PersonBriefListDto>(_Mapper.ConfigurationProvider).FirstOrDefault();

        //    if (exist is null || string.IsNullOrEmpty(exist?.MobileNumber))
        //        return new ApiResult<ForgetPasswordListDto>(false, ApiResultStatusCode.NotFound, null);

        //    var requestsCount = await Repository.TableNoTracking.Where(x => x.FPersonsId == exist.Id && x.IssueDate > DateTime.Now.AddHours(-24)).CountAsync();

        //    var lastRequestCode = await Repository.TableNoTracking.Where(x => x.FPersonsId == exist.Id)
        //          .OrderByDescending(x => x.IssueDate).FirstOrDefaultAsync();

        //    if (requestsCount >= AllowedNumberRequest || lastRequestCode?.IssueDate > DateTime.Now.AddMinutes(AllowedRequestTime))
        //        return new ApiResult<ForgetPasswordListDto>(false, ApiResultStatusCode.ToManyRequestAllowedInForgetPassword, null);

        //    Random random = new Random();

        //    int Code = random.Next(10000, 99999);

        //    var entity = new HrForgetPassword()
        //    {
        //        Code = Code,
        //        IssueDate = DateTime.Now,
        //        FPersonsId = exist.Id,
        //        IsActive = true
        //    };

        //    await Repository.AddAsync(entity, cancellationToken);

        //    string[] ret = ClsSend.SendSMS_Single("کد برای تغییر رمز عبور: " + Code.ToString(),
        //                                                exist.MobileNumber,
        //                                                "10008457",
        //                                                "web_contosoco",
        //                                                "1084pwe4",
        //                                                "http://193.104.22.14:2055/CPSMSService/Access",
        //                                                "contosoCO",
        //                                                false);

        //    var result = await Repository.TableNoTracking.Where(x => x.Id == entity.Id)
        //        .ProjectTo<ForgetPasswordListDto>(_Mapper.ConfigurationProvider).FirstOrDefaultAsync();

        //    if (ret[0] == "CHECK_OK")
        //    {
        //        return new ApiResult<ForgetPasswordListDto>(true, ApiResultStatusCode.Success, result, "کد احراز هویت با موفقیت ایجاد شد.");
        //    }
        //    else
        //        return new ApiResult<ForgetPasswordListDto>(false, ApiResultStatusCode.InsertFailed, result, "ارسال کد با مشکل مواجه شد.");

        //}

    }
}
