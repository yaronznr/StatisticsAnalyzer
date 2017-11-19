<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MainPage.aspx.cs" Inherits="WebUI.MainPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
	<script type="text/javascript" src="js/jquery.min.js"></script>
	<script type="text/javascript" src="js/flot/jquery.flot.min.js"></script>
    <style type="text/css">
        .auto-style1 {
            text-decoration: none;
        }
    </style>
</head>
<head>
    <meta charset="utf-8" />
    <title>Statistics Analyzer</title>
</head>
<body>
    <header>
                <div id="chrome" style = "float:right">
<%-- ReSharper disable Html.Obsolete --%>
                    <iframe allowTransparency frameborder="no" id ="Iframe1" name ="test" src="/UploadExcelFile.aspx">
<%-- ReSharper restore Html.Obsolete --%>
                    </iframe>
                </div>
        <div class="content-wrapper">
            <div class="float-left">
                <h1><a href="/MainPage.aspx" class="auto-style1">Statistics Analyzer</a></h1>
                <p>Quickly create, analyze and browse mixed-models and datasets.</p>
            </div>
        </div>
    </header>
    <form id="form1" runat="server">
        <asp:TextBox ID="FormulaText" runat="server" Width="65%"></asp:TextBox>
        <asp:Button ID="AnalyzeButton" runat="server" style="margin-left: 8px" Text="Analyze" Width="142px" OnClick="AnalyzeButton_Click"/>
	
    <br />
    <!-- div class="demo-container">
        <div id="placeholder"
             style="height:200px;width:60%;"
             class="demo-placeholder"></div>
    </!-->
    <div ID="div1" style = "margin-top: 20px;">
        <asp:Label ID="InterpertModel" runat="server" Width="60%"></asp:Label>
    </div>
    <div ID="div3" style = "margin-top: 20px;">
        <asp:Label ID="ModelAnswer" runat="server" Width="60%"></asp:Label>
    </div>
    <div ID="div2" style = "margin-top: 20px; font-family:monospace;">
        <asp:Label ID="AnalyzedModel" runat="server"></asp:Label>
    </div>
    <div ID="div4" style = "margin-top: 20px; font-family:monospace;">
        <asp:Label ID="Anova" runat="server"></asp:Label>
    </div>

    </form>
	
    </body>
</html>
