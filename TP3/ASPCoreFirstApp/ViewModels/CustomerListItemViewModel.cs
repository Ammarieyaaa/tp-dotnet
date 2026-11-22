namespace ASPCoreFirstApp.ViewModels
{
    // ViewModel de la liste des clients : uniquement les colonnes affichées.
    public class CustomerListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Email { get; set; }
        public string MembershipName { get; set; } = "";
        public int DiscountPercent { get; set; }
    }
}
