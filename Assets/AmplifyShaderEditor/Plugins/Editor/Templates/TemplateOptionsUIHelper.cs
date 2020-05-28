// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateOptionsUIHelper
	{
		private const string CustomOptionsLabel = " Custom Options";

		private bool m_isSubShader = false;

		[SerializeField]
		private bool m_passCustomOptionsFoldout = true;

		[SerializeField]
		private string m_passCustomOptionsLabel = CustomOptionsLabel;

		[SerializeField]
		private int m_passCustomOptionsSizeCheck = 0;

		[SerializeField]
		private List<TemplateOptionUIItem> m_passCustomOptionsUI = new List<TemplateOptionUIItem>();

		[NonSerialized]
		private Dictionary<string, TemplateOptionUIItem> m_passCustomOptionsUIDict = new Dictionary<string, TemplateOptionUIItem>();

		[NonSerialized]
		private TemplateMultiPassMasterNode m_owner;

		[NonSerialized]
		private string[] m_readOptionNames;

		[NonSerialized]
		private int[] m_readOptionSelections;

		[SerializeField]
		private List<TemplateOptionPortItem> m_passCustomOptionsPorts = new List<TemplateOptionPortItem>();
		public TemplateOptionsUIHelper( bool isSubShader )
		{
			m_isSubShader = isSubShader;
		}

		public void Destroy()
		{
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUI = null;

			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsUIDict = null;

			m_passCustomOptionsPorts.Clear();
			m_passCustomOptionsPorts = null;
		}

		public void DrawCustomOptions( TemplateMultiPassMasterNode owner )
		{
			m_owner = owner;
			if( m_passCustomOptionsSizeCheck > 0 )
			{
				NodeUtils.DrawNestedPropertyGroup( ref m_passCustomOptionsFoldout, m_passCustomOptionsLabel, DrawCustomOptionsBlock );
			}
		}

		public void DrawCustomOptionsBlock()
		{
			float currWidth = EditorGUIUtility.labelWidth;
			float size = Mathf.Max( UIUtils.CurrentWindow.ParametersWindow.TransformedArea.width - 125, 0 );
			EditorGUIUtility.labelWidth = size;
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Draw( m_owner );
			}
			EditorGUILayout.Space();
			EditorGUIUtility.labelWidth = currWidth;
		}

		public void OnCustomOptionSelected( bool isRefreshing, bool invertAction, TemplateMultiPassMasterNode owner, TemplateOptionUIItem uiItem, params TemplateActionItem[] validActions )
		{
			uiItem.CheckOnExecute = false;
			for( int i = 0; i < validActions.Length; i++ )
			{
				AseOptionsActionType actionType = validActions[ i ].ActionType;
				if( invertAction )
				{
					if( !TemplateOptionsToolsHelper.InvertAction( validActions[ i ].ActionType, ref actionType ) )
					{
						continue;
					}
				}

				switch( actionType )
				{
					case AseOptionsActionType.ShowOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
							}
							item.IsVisible = true;
							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action {1}", validActions[ i ].ActionData, validActions[ i ].ActionType );
						}
					}
					break;
					case AseOptionsActionType.HideOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							bool flag = false;
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
							}

							item.IsVisible = false || flag;
							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action {1}", validActions[ i ].ActionData, validActions[ i ].ActionType );
						}
					}
					break;
					case AseOptionsActionType.SetOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							item.CurrentOption = validActions[ i ].ActionDataIdx;
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action {1}", validActions[ i ].ActionData, validActions[ i ].ActionType );
						}
					}
					break;
					case AseOptionsActionType.HidePort:
					{
						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
						}

						if( passMasterNode != null )
						{
							InputPort port = validActions[ i ].ActionDataIdx > -1 ?
								passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx ) :
								passMasterNode.InputPorts.Find( x => x.Name.Equals( validActions[ i ].ActionData ) );
							if( port != null )
							{
								bool flag = false;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false || port.IsConnected );
								}

								port.Visible = false || flag;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action {2}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
						}
					}
					break;

					case AseOptionsActionType.ShowPort:
					{
						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
						}

						if( passMasterNode != null )
						{
							InputPort port = validActions[ i ].ActionDataIdx > -1 ?
								passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx ) :
								passMasterNode.InputPorts.Find( x => x.Name.Equals( validActions[ i ].ActionData ) );
							if( port != null )
							{
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
								}

								port.Visible = true;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action {2}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
						}
					}
					break;
					case AseOptionsActionType.SetPortName:
					{
						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
						}

						if( passMasterNode != null )
						{
							InputPort port = passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx );
							if( port != null )
							{
								port.Name = validActions[ i ].ActionData;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action {2}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
						}
					}
					break;
					case AseOptionsActionType.SetDefine:
					{
						//Debug.Log( "DEFINE "+validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#define " + validActions[ i ].ActionData;
							if( isRefreshing )
							{
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.MultiPassMasterNodes.NodesList;
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDefine( defineValue, false );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
							if( passMasterNode != null )
							{
								string defineValue = "#define " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDefine( defineValue, false );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveDefine:
					{
						//Debug.Log( "UNDEFINE " + validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#define " + validActions[ i ].ActionData;

							bool flag = false;
							if( isRefreshing )
							{
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.MultiPassMasterNodes.NodesList;
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDefine( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
							if( passMasterNode != null )
							{
								string defineValue = "#define " + validActions[ i ].ActionData;
								bool flag = false;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
								}
								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDefine( defineValue );
								}
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.SetUndefine:
					{
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							if( isRefreshing )
							{
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.MultiPassMasterNodes.NodesList;
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDefine( defineValue, false );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
							if( passMasterNode != null )
							{
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDefine( defineValue, false );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveUndefine:
					{
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							bool flag = false;
							if( isRefreshing )
							{
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.MultiPassMasterNodes.NodesList;
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDefine( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
							if( passMasterNode != null )
							{
								bool flag = false;
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
								}

								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDefine( defineValue );
								}
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.ExcludePass:
					{
						string optionId = validActions[ i ].ActionData + "Pass";
						bool flag = isRefreshing ? owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false ) : false;
						if( !flag )
							owner.SetPassVisible( validActions[ i ].ActionData, false );
					}
					break;
					case AseOptionsActionType.IncludePass:
					{
						string optionId = validActions[ i ].ActionData + "Pass";
						owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
						owner.SetPassVisible( validActions[ i ].ActionData, true );
					}
					break;
					case AseOptionsActionType.SetPropertyOnPass:
					{
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing )
							continue;

						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName );
							if( passMasterNode != null )
							{
								passMasterNode.SetPropertyActionFromItem( passMasterNode.PassModule, validActions[ i ] );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} on {2}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData );
							}
						}
						else
						{
							owner.SetPropertyActionFromItem( owner.PassModule, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.SetPropertyOnSubShader:
					{
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing )
							continue;

						owner.SetPropertyActionFromItem( owner.SubShaderModule, validActions[ i ] );
					}
					break;
					case AseOptionsActionType.SetShaderProperty:
					{
						//This action is only check when shader is compiled over 
						//the TemplateMultiPassMasterNode via the on CheckPropertyChangesOnOptions() method
					}
					break;
				}
			}
		}

		public void SetupCustomOptionsFromTemplate( TemplateMultiPassMasterNode owner, bool newTemplate )
		{
			TemplateOptionsContainer customOptionsContainer = m_isSubShader ? owner.SubShader.CustomOptionsContainer : owner.Pass.CustomOptionsContainer;

			if( !newTemplate && customOptionsContainer.Body.Length == m_passCustomOptionsSizeCheck )
			{
				for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
				{
					if( m_passCustomOptionsUI[ i ].EmptyEvent )
					{
						if( m_isSubShader )
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
						}
						else
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
						}
					}
				}
				return;
			}

			m_passCustomOptionsLabel = string.IsNullOrEmpty( customOptionsContainer.Name ) ? CustomOptionsLabel : " " + customOptionsContainer.Name;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsPorts.Clear();

			if( customOptionsContainer.Enabled )
			{
				m_passCustomOptionsSizeCheck = customOptionsContainer.Body.Length;
				for( int i = 0; i < customOptionsContainer.Options.Length; i++ )
				{
					switch( customOptionsContainer.Options[ i ].Type )
					{
						case AseOptionsType.Option:
						{
							TemplateOptionUIItem item = new TemplateOptionUIItem( customOptionsContainer.Options[ i ] );
							if( m_isSubShader )
							{
								item.OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
							}
							else
							{
								item.OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
							}

							m_passCustomOptionsUI.Add( item );
							m_passCustomOptionsUIDict.Add( customOptionsContainer.Options[ i ].Id, item );
						}
						break;
						case AseOptionsType.Port:
						{
							TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							m_passCustomOptionsPorts.Add( item );
							//if( m_isSubShader )
							//{
							//	if( string.IsNullOrEmpty( customOptionsContainer.Options[ i ].Id ) )
							//	{
							//		//No pass name selected. inject on all passes
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//	else if( customOptionsContainer.Options[ i ].Id.Equals( owner.PassName ) )
							//	{
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//}
							//else
							//{
							//	TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//	m_passCustomOptionsPorts.Add( item );
							//}
						}
						break;
					}
				}
			}
			else
			{
				m_passCustomOptionsSizeCheck = 0;
			}
		}

		public void SetCustomOptionsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector )
		{
			if( masterNode == null )
				return;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].FillDataCollector( ref dataCollector );
			}

			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			}
		}

		public void CheckImediateActionsForPort( TemplateMultiPassMasterNode masterNode , int portId )
		{
			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].CheckImediateActionsForPort( masterNode, portId );
			}
		}

		public void SetSubShaderCustomOptionsPortsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector  )
		{
			if( masterNode == null )
				return;

			
			//for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			//{
			//	if( string.IsNullOrEmpty( m_passCustomOptionsPorts[ i ].Options.Id ) ||
			//		masterNode.PassUniqueName.Equals( m_passCustomOptionsPorts[ i ].Options.Id ) )
			//	{
			//		m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			//	}
			//}
			
			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{	
				m_passCustomOptionsPorts[ i ].SubShaderFillDataCollector( masterNode, ref dataCollector );	
			}
		}

		public void RefreshCustomOptionsDict()
		{
			if( m_passCustomOptionsUIDict.Count != m_passCustomOptionsUI.Count )
			{
				m_passCustomOptionsUIDict.Clear();
				int count = m_passCustomOptionsUI.Count;
				for( int i = 0; i < count; i++ )
				{
					m_passCustomOptionsUIDict.Add( m_passCustomOptionsUI[ i ].Options.Id, m_passCustomOptionsUI[ i ] );
				}
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			RefreshCustomOptionsDict();
			int savedOptions = Convert.ToInt32( nodeParams[ index++ ] );

			m_readOptionNames = new string[ savedOptions ];
			m_readOptionSelections = new int[ savedOptions ];

			for( int i = 0; i < savedOptions; i++ )
			{
				string optionName = nodeParams[ index++ ];
				int optionSelection = Convert.ToInt32( nodeParams[ index++ ] );
				m_readOptionNames[ i ] = optionName;
				m_readOptionSelections[ i ] = optionSelection;

			}
		}

		public void SetReadOptions()
		{
			if( m_readOptionNames != null && m_readOptionSelections != null )
			{
				for( int i = 0; i < m_readOptionNames.Length; i++ )
				{
					if( m_passCustomOptionsUIDict.ContainsKey( m_readOptionNames[ i ] ) )
					{
						m_passCustomOptionsUIDict[ m_readOptionNames[ i ] ].CurrentOptionIdx = m_readOptionSelections[ i ];
					}
				}
			}
		}

		public void Refresh()
		{
			int count = m_passCustomOptionsUI.Count;
			for( int i = 0; i < count; i++ )
			{
				m_passCustomOptionsUI[ i ].Refresh();
			}
		}

		public void CheckDisable()
		{
			int count = m_passCustomOptionsUI.Count;
			for( int i = 0; i < count; i++ )
			{
				m_passCustomOptionsUI[ i ].CheckDisable();
			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			int optionsCount = m_passCustomOptionsUI.Count;
			IOUtils.AddFieldValueToString( ref nodeInfo, optionsCount );
			for( int i = 0; i < optionsCount; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_passCustomOptionsUI[ i ].Options.Id );
				IOUtils.AddFieldValueToString( ref nodeInfo, m_passCustomOptionsUI[ i ].CurrentOption );
			}
		}

		public bool HasCustomOptions { get { return m_passCustomOptionsSizeCheck > 0; } }
		public List<TemplateOptionUIItem> PassCustomOptionsUI { get { return m_passCustomOptionsUI; } }

	}
}