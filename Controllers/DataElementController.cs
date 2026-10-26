using System.ComponentModel.DataAnnotations;
using DPDP_Api.Data;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
    [Route("api/data-element")]
    public sealed class DataElementController : ControllerBase
    {
        private readonly Db _db;

        public DataElementController(Db db)
        {
            _db = db;
        }

        private int ScopeOrganisation(int? requestedOrganisationId)
        {
            if (User.RoleCode() == "SUPER_ADMIN")
            {
                if (!requestedOrganisationId.HasValue || requestedOrganisationId.Value <= 0)
                    throw new ArgumentException("Organisation selection is required.");

                return requestedOrganisationId.Value;
            }

            var organisationId = User.OrganisationId();

            if (!organisationId.HasValue)
                throw new UnauthorizedAccessException(
                    "Organisation access is not available for this account.");

            if (requestedOrganisationId.HasValue &&
                requestedOrganisationId.Value > 0 &&
                requestedOrganisationId.Value != organisationId.Value)
            {
                throw new UnauthorizedAccessException("Access denied.");
            }

            return organisationId.Value;
        }

        private ActionResult<ApiResponse<object>> Success(
            string message,
            object? data = null)
        {
            return Ok(ApiResponse<object>.Ok(
                200,
                message,
                data,
                HttpContext.TraceIdentifier));
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<object>>> Create(
            DataElementRequest request,
            CancellationToken ct)
        {
            var organisationId = ScopeOrganisation(request.OrganisationId);

            var id = await _db.ScalarIntAsync(
                "dbo.create_data_element",
                new[]
                {
                _db.P("@OrganisationId", organisationId),
                _db.P("@DataElementCode", request.DataElementCode.Trim()),
                _db.P("@DataElementName", request.DataElementName.Trim()),
                _db.P("@DataType", request.DataType),
                _db.P("@DataCategory", request.DataCategory),
                _db.P("@Description", request.Description),
                _db.P("@IsSensitive", request.IsSensitive),
                _db.P("@IsChildData", request.IsChildData),
                _db.P("@CreatedBy", User.UserId())
                },
                ct);

            return Success(
                "Data element created successfully.",
                new { dataElementId = id });
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<object>>> List(
            int? organisationId = null,
            string? search = null,
            string? dataCategory = null,
            bool? isActive = null,
            CancellationToken ct = default)
        {
            var orgId = ScopeOrganisation(organisationId);

            var rows = await _db.RowsAsync(
                "dbo.list_data_element",
                new[]
                {
                _db.P("@OrganisationId", orgId),
                _db.P("@Search", search),
                _db.P("@DataCategory", dataCategory),
                _db.P("@IsActive", isActive)
                },
                ct);

            return Success("Data elements retrieved successfully.", rows);
        }

        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<object>>> Update(
            UpdateDataElementRequest request,
            CancellationToken ct)
        {
            var organisationId = ScopeOrganisation(request.OrganisationId);

            await _db.ExecuteAsync(
                "dbo.update_data_element",
                new[]
                {
                _db.P("@DataElementId", request.DataElementId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@DataElementCode", request.DataElementCode.Trim()),
                _db.P("@DataElementName", request.DataElementName.Trim()),
                _db.P("@DataType", request.DataType),
                _db.P("@DataCategory", request.DataCategory),
                _db.P("@Description", request.Description),
                _db.P("@IsSensitive", request.IsSensitive),
                _db.P("@IsChildData", request.IsChildData),
                _db.P("@IsActive", request.IsActive),
                _db.P("@ModifiedBy", User.UserId())
                },
                ct);

            return Success("Data element updated successfully.");
        }

        [HttpDelete("delete/{dataElementId:int}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(
            int dataElementId,
            int? organisationId = null,
            CancellationToken ct = default)
        {
            var orgId = ScopeOrganisation(organisationId);

            await _db.ExecuteAsync(
                "dbo.delete_data_element",
                new[]
                {
                _db.P("@DataElementId", dataElementId),
                _db.P("@OrganisationId", orgId),
                _db.P("@ModifiedBy", User.UserId())
                },
                ct);

            return Success("Data element deleted successfully.");
        }
    }
}
