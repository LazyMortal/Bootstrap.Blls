﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bootstrap.Business.Extensions.ResponseBuilders;
using Bootstrap.Infrastructures.Components.Extensions;
using Bootstrap.Infrastructures.Models;
using Bootstrap.Infrastructures.Models.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Bootstrap.Business.Components.Services
{
    public class
        MultilevelResourceService<TDbContext, TDefaultResource> : AbstractService<TDbContext, TDefaultResource>
        where TDbContext : DbContext where TDefaultResource : MultilevelResource<TDefaultResource>
    {
        public MultilevelResourceService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<TDefaultResource>> GetPath(int id)
        {
            var resource = await GetByKey(id);
            return resource == null ? null : GetPath(DbContext.Set<TDefaultResource>(), resource);
        }

        protected List<TDefaultResource> GetPath(IEnumerable<TDefaultResource> allResources, TDefaultResource child)
        {
            if (child == null)
            {
                return null;
            }

            var path = allResources.Where(t => t.Left < child.Left && t.Right > child.Right).ToList();
            path.Add(child);
            return path.OrderBy(t => t.Left).ToList();
        }

        public async Task<List<TDefaultResource>> GetAll()
        {
            var data = await base.GetAll();
            var root = data.FindAll(t => !t.ParentId.HasValue);
            _populateTree(root, data);
            return root;
        }

        private void _populateTree(List<TDefaultResource> parents, List<TDefaultResource> allData)
        {
            if (parents != null && allData != null)
            {
                foreach (var p in parents)
                {
                    p.Children = allData.FindAll(t => t.ParentId == p.Id);
                    p.Children.ForEach(t => t.Parent = p);
                    _populateTree(p.Children, allData);
                }
            }
        }

        /// <summary>
        /// 异步刷新分级关系
        /// </summary>
        /// <returns></returns>
        protected async Task BuildTree()
        {
            var tree = await GetAll();
            tree.BuildTree();
            await DbContext.SaveChangesAsync();
        }

        public override async Task<BaseResponse> Delete(Expression<Func<TDefaultResource, bool>> selector)
        {
            var rsp = await base.Delete(selector);
            await DbContext.SaveChangesAsync();
            await BuildTree();
            return rsp;
        }

        public override async Task<SingletonResponse<TDefaultResource>> Create(TDefaultResource resource)
        {
            var rsp = await base.Create(resource);
            await BuildTree();
            return rsp;
        }


        public override async Task<BaseResponse> DeleteByKey(object key)
        {
            var rsp = await base.DeleteByKey(key);
            await DbContext.SaveChangesAsync();
            await BuildTree();
            return rsp;
        }

        public override async Task<BaseResponse> DeleteByKeys(IEnumerable<object> keys)
        {
            var rsp = await base.DeleteByKeys(keys);
            await DbContext.SaveChangesAsync();
            await BuildTree();
            return rsp;
        }

        public override async Task<SingletonResponse<TResource>> Create<TResource>(TResource resource)
        {
            var rsp = await base.Create(resource);
            await BuildTree();
            return rsp;
        }
    }
}