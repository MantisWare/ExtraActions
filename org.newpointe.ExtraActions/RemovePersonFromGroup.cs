﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;

namespace org.newpointe.ExtraActions
{
    /// <summary>
    /// Removes person from a group using a workflow attribute.
    /// </summary>
    [ActionCategory( "Extra Actions" )]
    [Description( "Removes a person from a group." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Advanced Group Member Remove" )]

    [PersonField( "Person", "The Person to remove from the Group.", false, "", "", 0 )]
    [WorkflowAttribute( "Person Attribute", "Workflow Attribute that contains the Person to remove from the Group.", false, "", "", 1, null, new[] { "Rock.Field.Types.PersonFieldType" } )]

    [GroupField( "Group", "The Group to remove the Person from.", false, "", "", 2 )]
    [WorkflowAttribute( "Group Attribute", "Workflow Attribute that contains the Group to remove the Person from.", false, "", "", 3, null, new[] { "Rock.Field.Types.GroupFieldType" } )]

    [GroupRoleField( "", "Group Role", "The Group Role to remove the Person from (Leave these options blank to remove any roles).", false, "", "", 4 )]
    [WorkflowAttribute( "Group Role Attribute", "Workflow Attribute that contains the Group Role to remove the Person from (Leave these options blank to remove any roles).", false, "", "", 5, null, new[] { "Rock.Field.Types.GroupRoleFieldType" } )]

    public class RemovePersonFromGroup : ExtraActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var person = GetEntityFromWorkflowAttributes( action, "Person", "PersonAttribute", new PersonAliasService( rockContext ) )?.Person;
            var group = GetEntityFromWorkflowAttributes( action, "Group", "GroupAttribute", new GroupService( rockContext ) );
            var groupRole = GetEntityFromWorkflowAttributes( action, "GroupRole", "GroupRoleAttribute", new GroupTypeRoleService( rockContext ) );

            if ( person == null )
                errorMessages.Add( "Invalid Person." );

            if ( group == null )
                errorMessages.Add( "Invalid Group." );
            
            if ( !errorMessages.Any() )
            {
                var groupMemberService = new GroupMemberService( rockContext );

                var groupMemberships = groupMemberService.Queryable().Where( gm => gm.GroupId == group.Id && gm.PersonId == person.Id );

                if( groupRole != null)
                {
                    groupMemberships = groupMemberships.Where( gm => gm.GroupRoleId == groupRole.Id );
                }

                try
                {
                    foreach ( var groupMember in groupMemberships )
                    {
                        groupMemberService.Delete( groupMember );
                    }

                    rockContext.SaveChanges();
                }
                catch ( Exception ex )
                {
                    errorMessages.Add($"An error occurred while removing the group memberships(s): {ex.Message}");
                }
            }
            
            errorMessages.ForEach( m => action.AddLogEntry( m, true ) );

            return true;
        }
        
    }
}