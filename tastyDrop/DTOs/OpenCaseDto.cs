namespace tastyDrop.Api.DTOs
{
    //когда юзер нажимает открыть кейс 
    public class OpenCaseDto
    {
        //id кейса который выбрал юзер 
        public int CaseId { get; set; }

        //мультикаст кейса x2 x3...
        public int Amount { get; set; } = 1;
    }
}
