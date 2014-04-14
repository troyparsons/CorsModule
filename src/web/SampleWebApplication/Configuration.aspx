<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Configuration.aspx.cs" Inherits="SampleWebApplication.Configuration" %>
<%@ Import Namespace="Cors" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title></title>
</head>
<body>
	<form id="form1" runat="server">
		<div>
		<ul>
			<li>Allow Credentials: <%= Config.AllowCredentials %></li>
			<li>Allow Headers: <%= Config.AllowHeaders %></li>
			<li>Expose Headers: <%= Config.ExposeHeaders %></li>
			<li>Pre-flight Response Max Age: <%= Config.PreflightCacheMaxAge %></li>
			<li>Origins:
				<ul>
					<% foreach (OriginConfigurationElement origin in Config.Origins)
						{ %>
						<li><%= origin.Origin %></li>
					<% } %>
				</ul>
			</li>
			<li>Resources:
				<ul>
					<% foreach (ResourceConfigurationElement resource in Config.Resources)
						{ %>
						<li><%= resource.Path %>
							<ul>
								<li>Allow Methods: <%= resource.AllowMethods %></li>
								<li>Allow Headers: <%= resource.AllowHeaders %> (effective: <%= string.Join(", ", Config.GetResourceAllowHeaders(resource.Path)) %>)</li>
								<li>Expose Headers: <%= resource.ExposeHeaders %> (effective: <%= string.Join(", ", Config.GetResourceExposeHeaders(resource.Path)) %>)</li>
							</ul>
						</li>
					<% } %>
				</ul>
			</li>
		</ul>
	</form>
</body>
</html>