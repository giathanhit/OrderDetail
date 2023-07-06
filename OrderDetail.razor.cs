using Blazorise;
using DevExpress.Blazor;
using DevExpress.Blazor.Internal;
using DevExpress.Utils.About;
using DevExpress.XtraPrinting;
using HqSoftSale.Blazor.Components;
using HqSoftSale.OrderDetails;
using HqSoftSale.Orders;
using HqSoftSale.Products;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Xml.Linq;
using Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;
using Volo.Abp.Users;

namespace HqSoftSale.Blazor.Pages.Orders
{
    public partial class OrderDetail
    {
        bool IsOpen { get; set; } = false;
        private IReadOnlyList<ProductDto> products { get; set; }

        protected IReadOnlyList<ProductDto> NewDetailList = new List<ProductDto>();

        protected Validations CreateValationRef;

        protected CreateUpdateOrderDto NewEntity = new();

        protected CreateUpdateOrdDetailsDto NewDetailEntity = new();
        private int PageSize { get; set; } = 1000;
        private int CurrentPage { get; set; }
        private string CurrentSorting { get; set; }
        private List<CreateUpdateOrdDetailsDto> productDtoList { get; set; }

        IGrid Grid { get; set; }

        protected override async Task OnInitializedAsync()
        {
            productDtoList = new List<CreateUpdateOrdDetailsDto>();
            await base.OnInitializedAsync();
            if (CreateValationRef != null)
            {
                await CreateValationRef.ClearAll();
            }

            NewEntity.OrderNumber = await OrderAppService.GenerateOrderIdAsync();
            NewDetailEntity.OrderID = await OrderAppService.GenerateOrderIdAsync();
            await CalculatePrice();
            await GetProductAsync();
        }

        protected virtual async Task CreateEntityAsync()
        {
            try
            {
                var validate = true;
                if (CreateValationRef != null)
                {
                    validate = await CreateValationRef.ValidateAll();
                }
                if (validate)
                {
                    OrderAppService.CreateAsync(NewEntity);
                    OrderDetailAppService.CreateAsync(NewDetailEntity);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
            }
        }
         
        //Datagrid DetailComponent

        List<(string Field, string Caption)> columns = new List<(string Field, string Caption)>
        {
            ("ProductID", "Product ID"),
            ("ProductName", "Product Name"),
            ("UnitType", "Unit Type"),
            ("Type", "Type"),
            ("Price", "Price")
        };

        void OnRowDoubleClick(GridRowClickEventArgs e)
        {
            NewDetailEntity.ProductID = $"{e.Grid.GetRowValue(e.VisibleIndex, "ProductID")} ";
            NewDetailEntity.ProductName = $"{e.Grid.GetRowValue(e.VisibleIndex, "ProductName")} ";
            string priceS = $"{e.Grid.GetRowValue(e.VisibleIndex, "Price")} ";
            NewDetailEntity.Price = double.Parse(priceS);
        }

        //Popup thêm mới sản phẩm
        private PopupProduct PopupProduct;
        private bool IsCreatePopupVisible = false;
        private async Task ShowCreatePopup()
        {
            try
            {
                IsCreatePopupVisible = true;
                await PopupProduct.ShowPopupAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi mở popup: " + ex.Message);
            }
        }
        private void OnProductCreated()
        { 
            NavigationManager.NavigateTo("/order/new", forceLoad: true);
        }
          
        private async Task GetProductAsync()
        {
            var result = await ProductAppService.GetListAsync(
                new GetProductListDto
                {
                    MaxResultCount = PageSize,
                    SkipCount = CurrentPage * PageSize,
                    Sorting = CurrentSorting
                }
            );

            products = result.Items;
            TotalCount = (int)result.TotalCount;
        }

        private void UpdateTotal(int newQuantity)
        {
            NewDetailEntity.Quantity = newQuantity;
            NewDetailEntity.ExtenedAmount = NewDetailEntity.Quantity * NewDetailEntity.Price;
        }

        protected virtual async Task CalculatePrice()
        {
            if (!string.IsNullOrEmpty(NewDetailEntity.ProductID))
            {
                var product = products.FirstOrDefault(p => p.ProductID == NewDetailEntity.ProductID);
                if (product != null)
                {
                    NewDetailEntity.Price = NewDetailEntity.Quantity * product.Price;
                }
            }
        }
    }
}

