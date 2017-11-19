<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UploadExcelFile.aspx.cs" Inherits="WebUI.UploadExcelFile" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server" style="float:left">
    <div >
        <div id="FileDiv" runat="server"></div>
        <input type="file" id="File1" name="File1" runat="server" style="display:none" onchange="document.forms[0].submit()" />
        <input type="button"
               value="select new file..."
               style="background-color: transparent; text-decoration: underline; border: none; color: blue; cursor: pointer;"
               onclick="document.forms[0].File1.click()" />
    </div>
    </form>
</body>
</html>
