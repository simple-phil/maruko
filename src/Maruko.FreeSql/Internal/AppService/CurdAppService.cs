﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Maruko.Core.Application.Servers;
using Maruko.Core.Application.Servers.Dto;
using Maruko.Core.FreeSql.Internal.Repos;
using Maruko.Core.ObjectMapping;

namespace Maruko.Core.FreeSql.Internal.AppService
{
    public class CurdAppService<TEntity, TEntityDto, TSearch> : CurdAppServiceBase<TEntity, TEntityDto, TEntityDto, TEntityDto>, ICurdAppService<TEntity, TEntityDto, TSearch>
        where TEntity : FreeSqlEntity
        where TEntityDto : EntityDto
        where TSearch : PageDto
    {
        private readonly IFreeSqlRepository<TEntity> Table;

        public CurdAppService(IObjectMapper objectMapper, IFreeSqlRepository<TEntity> repository) : base(objectMapper, repository)
        {
            Table = repository;
        }

        public virtual PagedResultDto PageSearch(TSearch search)
        {
            var query = Table.GetAll().Select<TEntity>();

            query = query.Where(ConditionToLambda(search));

            query = OrderFilter() != null
                ? query.OrderByDescending(OrderFilter())
                : query.OrderByDescending(item => item.Id);

            var result = query
                .Count(out var total)
                .Page(search.PageIndex, search.PageMax)
                .ToList();

            return new PagedResultDto(total, ConvertToEntityDTOs(result));
        }

        public virtual TEntityDto CreateOrEdit(TEntityDto request)
        {
            TEntity data = null;
            if (request.Id == 0)
            {
                request.CreateTime = DateTime.Now;
                data = Table.Insert(MapToEntity(request));
            }
            else
            {
                data = Table.FirstOrDefault(item => item.Id == request.Id);
                data = MapToEntity(request);
                data.CreateTime = DateTime.Now;
                data = Table.Update(data);
            }

            return data == null
                ? null
                : ObjectMapper.Map<TEntityDto>(data);
        }

        protected Expression<Func<TEntity, bool>> ConditionToLambda(TSearch search)
        {
            Expression<Func<TEntity, bool>> expression = item => true;

            search.DynamicFilters.ForEach(filter =>
            {
                //filter.Field = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(filter.Field);

                var type = typeof(TEntity).GetProperty(filter.Field)?.PropertyType;

                expression = expression.And(CreateExpression(type, filter.Field, filter.Value, filter.Operate));

            });

            return expression;
        }

        protected Expression<Func<TEntity, bool>> CreateExpression(Type fieldType, string fieldName, object value, string operate)
        {
            var lambdaParam = Expression.Parameter(typeof(TEntity));

            Expression lambdaBody = default;

            if (operate == Condition.Equal.ToString())
                lambdaBody = Expression.Equal(
                   Expression.PropertyOrField(lambdaParam, fieldName),
                   Expression.Constant(value, fieldType)
               );

            else if (operate == Condition.NotEqual.ToString())
                lambdaBody = Expression.NotEqual(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    Expression.Constant(value, fieldType)
                );

            else if (operate == Condition.Like.ToString())
            {
                lambdaBody = Expression.Call(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    fieldType.GetMethod("Contains", new Type[] { fieldType })
                    , Expression.Constant(value, fieldType));
            }
            else if (operate == Condition.GreaterThan.ToString())
            {
                lambdaBody = Expression.GreaterThan(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    Expression.Constant(value, fieldType)
                );
            }
            else if (operate == Condition.GreaterThanOrEqual.ToString())
            {
                lambdaBody = Expression.GreaterThanOrEqual(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    Expression.Constant(value, fieldType)
                );
            }
            else if (operate == Condition.LessThan.ToString())
            {
                lambdaBody = Expression.LessThan(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    Expression.Constant(value, fieldType)
                );
            }
            else if (operate == Condition.LessThanOrEqual.ToString())
            {
                lambdaBody = Expression.LessThanOrEqual(
                    Expression.PropertyOrField(lambdaParam, fieldName),
                    Expression.Constant(value, fieldType)
                );
            }

            return Expression.Lambda<Func<TEntity, bool>>(lambdaBody, lambdaParam);
        }

        protected virtual Expression<Func<TEntity, int>> OrderFilter()
        {
            return null;
        }

        protected virtual List<TEntityDto> ConvertToEntityDTOs(List<TEntity> entities)
        {
            return ObjectMapper.Map<List<TEntityDto>>(entities);
        }
    }
}
