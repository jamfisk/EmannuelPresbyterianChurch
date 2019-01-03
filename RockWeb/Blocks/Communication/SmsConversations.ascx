﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SmsConversations.ascx.cs" Inherits="RockWeb.Blocks.Communication.SmsConversations" %>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-comments"></i> SMS Conversations</h1>
                <div class="panel-labels"> <!--  style="position:absolute;right:15px;top:10px;" -->
                    <a href="#" onclick="$('.js-sms-configuration').toggle()">
                        <i class="fa fa-cog"></i>
                    </a>
                </div>
            </div>

            <div class="js-sms-configuration panel-body" style="display: none">
                <%-- The list of phone numbers that do not have "Enable Mobile Conversations" enabled --%>
                <div class="col-md-3">
                    <Rock:Toggle ID="tglShowRead" runat="server" Label="Show Read" OnCheckedChanged="tglShowRead_CheckedChanged" OnText="Yes" OffText="No" Checked="true" ButtonSizeCssClass="btn-sm" />
                </div>
                <div class="col-md-3">
                    <Rock:RockDropDownList ID="ddlSmsNumbers" runat="server" Label="SMS Number" AutoPostBack="true" OnSelectedIndexChanged="ddlSmsNumbers_SelectedIndexChanged" CssClass="input-sm" />
                    <asp:Label ID="lblSelectedSmsNumber" runat="server" visible="false" />
                </div>
            </div>

            <div>
                <Rock:NotificationBox ID="nbNoNumbers" runat="server" NotificationBoxType="Warning" Text="No SMS numbers are available to view." Visible="false"></Rock:NotificationBox>

                <div class="sms-conversations">
                    <div class="messages">
                    <asp:LinkButton ID="btnCreateNewMessage" runat="server" CssClass="btn btn-default btn-block btn-new-message" OnClick="btnCreateNewMessage_Click"><i class="fa fa-comments"></i>&nbsp;New Message</asp:LinkButton>
                    <asp:UpdatePanel ID="upRecipients"  runat="server">
                        <ContentTemplate>
                            <Rock:Grid ID="gRecipients" runat="server" OnRowSelected="gRecipients_RowSelected" OnRowDataBound="gRecipients_RowDataBound" ShowHeader="false" ShowActionRow="false" DisplayType="Light" EnableResponsiveTable="False">
                                <Columns>
                                    <Rock:RockBoundField DataField="RecipientId" Visible="false"></Rock:RockBoundField>
                                    <Rock:RockTemplateField>
                                        <ItemTemplate>
                                            <div>
                                                <Rock:HiddenFieldWithClass ID="hfRecipientId" runat="server" CssClass="js-recipientId" Value='<%# Eval("RecipientId") %>' />
                                                <Rock:HiddenFieldWithClass ID="hfMessageKey" runat="server" CssClass="js-messageKey" Value='<%# Eval("MessageKey") %>' />

                                                <div class="layout-row">
                                                    <asp:Label ID="lblName" runat="server" Text='<%# Eval("FullName") ?? Eval("MessageKey") %>' Class="sms-name"></asp:Label>
                                                    <div class="sms-date flex-noshrink"><asp:Literal ID="litDateTime" runat="server" Text='<%# Eval("CreatedDateTime") %>'></asp:Literal></div>
                                                </div>
                                                <div class="message-truncate"><asp:Literal ID="litMessagePart" runat="server" Text='<%# Eval("LastMessagePart") %>'></asp:Literal></div>

                                                <asp:LinkButton ID="lbLinkConversation" runat="server" Text="Link To Person" Visible="false" CssClass="hidden" OnClick="lbLinkConversation_Click" CommandArgument='<%# Eval("MessageKey") %>'></asp:LinkButton>
                                            </div>
                                        </ItemTemplate>
                                    </Rock:RockTemplateField>
                                </Columns>
                            </Rock:Grid>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    </div>

                        <asp:UpdatePanel ID="upConversation" runat="server" class="conversations"><ContentTemplate>
                            <Rock:HiddenFieldWithClass ID="hfSelectedRecipientId" runat="server" CssClass="js-selected-recipient-id" />
                            <div class="header">
                            Person Name
                            </div>
                            <div class="messages-outer-container">
                                <div class="messages-container">
                                    <asp:Repeater ID="rptConversation" runat="server" OnItemDataBound="rptConversation_ItemDataBound" Visible="false">
                                        <ItemTemplate>
                                            <div class="message by-us" id="divCommunication" runat="server">
                                                <Rock:HiddenFieldWithClass ID="hfCommunicationRecipientId" runat="server" Value='<%# Eval("FromPersonAliasId") %>' />
                                                <Rock:HiddenFieldWithClass ID="hfCommunicationMessageKey" runat="server" Value='<%# Eval("MessageKey") %>' />
                                                <div class="bubble bg-primary" id="divCommunicationBody" runat="server">
                                                <%# Eval("Response") %>
                                                </div>
                                                <div class="sms-send small">Ted Decker - <%# Eval("CreatedDateTime") %></div>
                                            </div>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            <asp:Label ID="lbNoConversationsFound" runat="server" Visible='<%# rptConversation.Items.Count == 0 %>' Text="<tr><td>No conversations found.</td></tr>" CssClass="text-muted" />
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>

                            <div class="footer">
                                <Rock:RockTextBox ID="tbNewMessage" runat="server" TextMode="multiline" Rows="1" Placeholder="Type a message" CssClass="js-input-message" autofocus></Rock:RockTextBox>
                                <Rock:BootstrapButton ID="btnSend" runat="server" CssClass="btn btn-primary js-send-text-button" Text="Send" OnClick="btnSend_Click"></Rock:BootstrapButton>
                            </div>

                        </ContentTemplate></asp:UpdatePanel>

                </div>

            </div>

        </div> <%-- End panel-block --%>

        <asp:HiddenField ID="hfActiveDialog" runat="server" />

        <Rock:ModalDialog ID="mdNewMessage" runat="server" Title="New Message" OnSaveClick="mdNewMessage_SaveClick" OnCancelScript="clearActiveDialog();" SaveButtonText="Send" ValidationGroup="vgMobileTextEditor">
            <Content>
                <asp:ValidationSummary ID="vsMobileTextEditor" runat="server" HeaderText="Please correct the following:" ValidationGroup="vgMobileTextEditor" CssClass="alert alert-validation" />
                <asp:Label ID="lblMdNewMessageSendingSMSNumber" runat="server" />
                <%-- person picker --%>
                <Rock:PersonPicker ID="ppRecipient" runat="server" Label="Recipient" ValidationGroup="vgMobileTextEditor" RequiredErrorMessage="Please select an SMS recipient." Required="true" />

                <%-- multi-line textbox --%>
                <Rock:RockTextBox ID="tbSMSTextMessage" runat="server" CssClass="js-sms-text-message" TextMode="MultiLine" Rows="3" Required="true" ValidationGroup="vgMobileTextEditor" RequiredErrorMessage="Message is required" ValidateRequestMode="Disabled" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdLinkConversation" runat="server" Title="Link to Person" OnSaveClick="mdLinkConversation_SaveClick" OnCancelScript="clearActiveDialog();">
            <Content>
                <asp:HiddenField ID="hfMessageKey" runat="server" />
                <asp:HiddenField ID="hfActiveTab" runat="server" />

                <ul class="nav nav-pills margin-b-md">
                    <li id="liNewPerson" runat="server" class="active"><a href='#<%=divNewPerson.ClientID%>' data-toggle="pill">Add New Person</a></li>
                    <li id="liExistingPerson" runat="server"><a href='#<%=divExistingPerson.ClientID%>' data-toggle="pill">Add Existing Person</a></li>
                </ul>

                <Rock:NotificationBox ID="nbAddPerson" runat="server" Heading="Please correct the following:" NotificationBoxType="Danger" Visible="false" />
                <asp:ValidationSummary ID="valSummaryAddPerson" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="AddPerson"/>

                <div class="tab-content">

                    <div id="divNewPerson" runat="server" class="tab-pane active">
                        <div class="row">
                            <div class="col-sm-4">
                                <div class="well">
                                    <Rock:DefinedValuePicker ID="dvpNewPersonTitle" runat="server" Label="Title" ValidationGroup="AddPerson" CssClass="input-width-md" />
                                    <Rock:RockTextBox ID="tbNewPersonFirstName" runat="server" Label="First Name" ValidationGroup="AddPerson" Required="true" autocomplete="off" />
                                    <Rock:RockTextBox ID="tbNewPersonLastName" runat="server" Label="Last Name" ValidationGroup="AddPerson" Required="true" autocomplete="off" />
                                    <Rock:DefinedValuePicker ID="dvpNewPersonSuffix" runat="server" Label="Suffix" ValidationGroup="AddPerson" CssClass="input-width-md" />
                                </div>
                            </div>
                            <div class="col-sm-4">
                                <div class="well">
                                    <Rock:DefinedValuePicker ID="dvpNewPersonConnectionStatus" runat="server" Label="Connection Status" ValidationGroup="AddPerson" Required="true"/>
                                    <Rock:RockRadioButtonList ID="rblNewPersonRole" runat="server" Required="true" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" Label="Role" ValidationGroup="AddPerson"/>
                                    <Rock:RockRadioButtonList ID="rblNewPersonGender" runat="server" Required="true" Label="Gender" RepeatDirection="Horizontal" ValidationGroup="AddPerson"/>
                                </div>
                            </div>
                            <div class="col-sm-4">
                                <div class="well">
                                    <Rock:DatePicker ID="dpNewPersonBirthDate" runat="server" Label="Birthdate" ValidationGroup="AddPerson" AllowFutureDateSelection="False" ForceParse="false"/>
                                    <Rock:GradePicker ID="ddlGradePicker" runat="server" Label="Grade" ValidationGroup="AddPerson" UseAbbreviation="true" UseGradeOffsetAsValue="true" />
                                    <Rock:DefinedValuePicker ID="dvpNewPersonMaritalStatus" runat="server" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" Label="Marital Status"  ValidationGroup="AddPerson"/>
                                </div>
                            </div>
                        </div>

                    </div>

                    <div id="divExistingPerson" runat="server" class="tab-pane">
                        <fieldset>
                            <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" Required="true" ValidationGroup="AddPerson" />
                        </fieldset>
                    </div>

                </div>

            </Content>
        </Rock:ModalDialog>

        <script>
            Sys.Application.add_load(function () {
                var objDiv = $(".messages-outer-container")[0];
                objDiv.scrollTop = objDiv.scrollHeight;

                $('.js-input-message').keypress(function (e) {
                var key = e.which;
                if(key == 13)  // the enter key code
                {
                    $('.js-send-text-button').click();
                    return false;
                }
                });
            });

            function clearActiveDialog() {
                $('#<%=hfActiveDialog.ClientID %>').val('');
            }
        </script>
    </ContentTemplate>
</asp:UpdatePanel>