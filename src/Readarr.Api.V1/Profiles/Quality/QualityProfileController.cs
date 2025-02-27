using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Http.REST.Attributes;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Profiles.Quality
{
    [V1ApiController]
    public class QualityProfileController : RestController<QualityProfileResource>
    {
        private readonly IProfileService _profileService;

        public QualityProfileController(IProfileService profileService)
        {
            _profileService = profileService;
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
            SharedValidator.RuleFor(c => c.Items).ValidItems();
        }

        [RestPostById]
        public ActionResult<QualityProfileResource> Create(QualityProfileResource resource)
        {
            var model = resource.ToModel();
            model = _profileService.Add(model);
            return Created(model.Id);
        }

        [RestDeleteById]
        public void DeleteProfile(int id)
        {
            _profileService.Delete(id);
        }

        [RestPutById]
        public ActionResult<QualityProfileResource> Update(QualityProfileResource resource)
        {
            var model = resource.ToModel();

            _profileService.Update(model);

            return Accepted(model.Id);
        }

        protected override QualityProfileResource GetResourceById(int id)
        {
            return _profileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<QualityProfileResource> GetAll()
        {
            return _profileService.All().ToResource();
        }
    }
}
