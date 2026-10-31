using Xunit;
using ScipDashboard.Core.Models;
using ScipDashboard.Modules.DashboardSummary;
using System.Collections.Generic;

namespace ScipDashboard.Tests
{
    public class CoreTests
    {
        [Fact]
        public void Route_ShouldContainStops()
        {
            // Arrange
            var route = new Route 
            { 
                Id = "TestRoute", 
                Name = "Test Route", 
                Stops = new List<RouteStop> 
                { 
                    new RouteStop { LocationId = "LOC1", LocationName = "Stop 1" } 
                } 
            };

            // Act & Assert
            Assert.Single(route.Stops);
            Assert.Equal("Stop 1", route.Stops[0].LocationName);
        }

        [Fact]
        public void DashboardSummary_ShouldInitializeWithMockValues()
        {
            // Arrange & Act
            var vm = new DashboardSummaryViewModel();

            // Assert
            Assert.Equal(2, vm.ActiveHaulCount);
            Assert.Equal(1, vm.ActiveRouteCount);
            Assert.Equal(14, vm.FleetCount);
            Assert.Equal(4500, vm.CargoCapacity);
        }

        [Fact]
        public void Route_Loop_ShouldMatchFirstAndLastStop()
        {
            // Arrange
            var route = new Route 
            { 
                Id = "LoopRoute", 
                Name = "Loop Route", 
                IsLoop = true,
                Stops = new List<RouteStop> 
                { 
                    new RouteStop { LocationId = "START", LocationName = "Home" },
                    new RouteStop { LocationId = "MID", LocationName = "Market" },
                    new RouteStop { LocationId = "START", LocationName = "Home" }
                } 
            };

            // Act & Assert
            Assert.True(route.IsLoop);
            Assert.Equal(route.Stops[0].LocationId, route.Stops[2].LocationId);
        }
    }
}
