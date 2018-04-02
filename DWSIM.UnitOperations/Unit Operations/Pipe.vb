﻿'    Pipe Calculation Routines 
'    Copyright 2008 Daniel Wagner O. de Medeiros
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports DWSIM.DrawingTools.GraphicObjects
Imports DWSIM.Thermodynamics
Imports DWSIM.Thermodynamics.Streams
Imports DWSIM.SharedClasses
Imports System.Windows.Forms
Imports DWSIM.UnitOperations.UnitOperations.Auxiliary
Imports DWSIM.UnitOperations.UnitOperations.Auxiliary.Pipe
Imports DWSIM.Thermodynamics.BaseClasses
Imports DWSIM.Interfaces.Enums
Imports DWSIM.Thermodynamics.PropertyPackages.Auxiliary
Imports OxyPlot
Imports OxyPlot.Axes

Imports cv = DWSIM.SharedClasses.SystemsOfUnits.Converter

Namespace UnitOperations

    Public Enum FlowPackage
        Beggs_Brill
        Lockhart_Martinelli
        Petalas_Aziz
    End Enum

    <System.Serializable()> Public Class Pipe

        Inherits UnitOperations.UnitOpBaseClass

        <NonSerialized> <Xml.Serialization.XmlIgnore> Dim f As EditingForm_Pipe

        Protected m_profile As New PipeProfile
        Protected m_thermalprofile As New ThermalEditorDefinitions
        Protected m_selectedflowpackage As FlowPackage = FlowPackage.Beggs_Brill
        Protected m_dp As Nullable(Of Double)
        Protected m_dt As Nullable(Of Double)
        Protected m_DQ As Nullable(Of Double)

        Protected m_tolP As Double = 100
        Protected m_tolT As Double = 0.01

        Protected m_maxItT As Integer = 50
        Protected m_maxItP As Integer = 50

        Protected m_flashP As Double = 1
        Protected m_flashT As Double = 1

        Protected m_includejteffect As Boolean = False

        Public Enum specmode
            Length = 0
            OutletPressure = 1
            OutletTemperature = 2
        End Enum

        Public Property Specification As specmode = specmode.Length
        Public Property OutletPressure As Double = 101325
        Public Property OutletTemperature As Double = 298.15

        Public Property IncludeJTEffect() As Boolean
            Get
                Return m_includejteffect
            End Get
            Set(ByVal value As Boolean)
                m_includejteffect = value
            End Set
        End Property

        Public Property MaxPressureIterations() As Integer
            Get
                Return m_maxItP
            End Get
            Set(ByVal value As Integer)
                m_maxItP = value
            End Set
        End Property

        Public Property MaxTemperatureIterations() As Integer
            Get
                Return m_maxItT
            End Get
            Set(ByVal value As Integer)
                m_maxItT = value
            End Set
        End Property

        Public Property TriggerFlashP() As Double
            Get
                Return m_flashP
            End Get
            Set(ByVal value As Double)
                m_flashP = value
            End Set
        End Property

        Public Property TriggerFlashT() As Double
            Get
                Return m_flashT
            End Get
            Set(ByVal value As Double)
                m_flashT = value
            End Set
        End Property

        Public Property TolP() As Double
            Get
                Return m_tolP
            End Get
            Set(ByVal value As Double)
                m_tolP = value
            End Set
        End Property

        Public Property TolT() As Double
            Get
                Return m_tolT
            End Get
            Set(ByVal value As Double)
                m_tolT = value
            End Set
        End Property

        Public Property DeltaP() As Nullable(Of Double)
            Get
                Return m_dp
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dp = value
            End Set
        End Property

        Public Property DeltaT() As Nullable(Of Double)
            Get
                Return m_dt
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_dt = value
            End Set
        End Property

        Public Property DeltaQ() As Nullable(Of Double)
            Get
                Return m_DQ
            End Get
            Set(ByVal value As Nullable(Of Double))
                m_DQ = value
            End Set
        End Property

        Public Property SelectedFlowPackage() As FlowPackage
            Get
                Return Me.m_selectedflowpackage
            End Get
            Set(ByVal value As FlowPackage)
                Me.m_selectedflowpackage = value
            End Set
        End Property

        Public Property Profile() As PipeProfile
            Get
                Return m_profile
            End Get
            Set(ByVal value As PipeProfile)
                m_profile = value
            End Set
        End Property

        Public Property ThermalProfile() As ThermalEditorDefinitions
            Get
                Return m_thermalprofile
            End Get
            Set(ByVal value As ThermalEditorDefinitions)
                m_thermalprofile = value
            End Set
        End Property

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal name As String, ByVal description As String)
            MyBase.CreateNew()
            Me.Profile = New PipeProfile
            Me.ThermalProfile = New ThermalEditorDefinitions
            Me.ComponentName = name
            Me.ComponentDescription = description
        End Sub

        Public Overrides Function CloneXML() As Object
            Return New Pipe().LoadData(Me.SaveData)
        End Function

        Public Overrides Function CloneJSON() As Object
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of Pipe)(Newtonsoft.Json.JsonConvert.SerializeObject(Me))
        End Function

        Public Overrides Sub Calculate(Optional ByVal args As Object = Nothing)

            Dim IObj As Inspector.InspectorItem = Inspector.Host.GetNewInspectorItem()

            Inspector.Host.CheckAndAdd(IObj, New StackFrame(1).GetMethod().Name, "Calculate", If(GraphicObject IsNot Nothing, GraphicObject.Tag, "Temporary Object") & " (" & GetDisplayName() & ")", GetDisplayName() & " Calculation Routine", True)

            IObj?.SetCurrent()

            If Not Me.GraphicObject.EnergyConnector.IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("NohcorrentedeEnergyFlow3"))
            ElseIf Not Me.Profile.Status = PipeEditorStatus.OK Then
                Throw New Exception(FlowSheet.GetTranslatedString("Operfilhidrulicodatu"))
            ElseIf Not Me.GraphicObject.OutputConnectors(0).IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            ElseIf Not Me.GraphicObject.InputConnectors(0).IsAttached Then
                Throw New Exception(FlowSheet.GetTranslatedString("Verifiqueasconexesdo"))
            End If

            If Me.Specification = specmode.OutletPressure Then
                If Me.Profile.Sections.Count > 1 Then
                    Throw New Exception(FlowSheet.GetTranslatedString("PipeOutletPressureRestriction"))
                ElseIf Me.Profile.Sections.Count = 1 Then
                    If Me.Profile.Sections(1).TipoSegmento <> "Tubulaosimples" Then
                        Throw New Exception(FlowSheet.GetTranslatedString("PipeOutletPressureRestriction"))
                    End If
                End If
            End If

            Dim fpp As FlowPackages.FPBaseClass

            Select Case Me.SelectedFlowPackage
                Case FlowPackage.Lockhart_Martinelli
                    fpp = New FlowPackages.LockhartMartinelli
                Case FlowPackage.Petalas_Aziz
                    fpp = New FlowPackages.PetalasAziz
                Case Else
                    fpp = New FlowPackages.BeggsBrill
            End Select

            Dim oms As MaterialStream

            Dim Tin, Pin, Tout, Pout, Tout_ant, Pout_ant, Pout_ant2, Toutj, Text, Win, Qin, Qvin, Qlin, TinP, PinP, _
                rho_l, rho_v, Cp_l, Cp_v, Cp_m, K_l, K_v, eta_l, eta_v, tens, Hin, Hout, HinP, _
                fT, fP, fP_ant, fP_ant2, w_v, w_l, w, z, z2, dzdT, dText_dL As Double
            Dim cntP, cntT As Integer

            If Me.Specification = specmode.OutletTemperature Then
                Me.ThermalProfile.TipoPerfil = ThermalEditorDefinitions.ThermalProfileType.Definir_Q
                Me.ThermalProfile.Calor_trocado = 0.0#
            End If

            If Me.ThermalProfile.TipoPerfil = ThermalEditorDefinitions.ThermalProfileType.Definir_CGTC Then
                Text = Me.ThermalProfile.Temp_amb_definir
                dText_dL = Me.ThermalProfile.AmbientTemperatureGradient
            Else
                Text = Me.ThermalProfile.Temp_amb_estimar
                dText_dL = Me.ThermalProfile.AmbientTemperatureGradient_EstimateHTC
            End If

            'Calcular DP
            Dim Tpe, Ppe, Tspec, Pspec As Double
            Dim resv As Object
            Dim equilibrio As Object = Nothing
            Dim tmp As Object = Nothing
            Dim tipofluxo As String
            Dim first As Boolean = True
            Dim holdup, dpf, dph, dpt, DQ, DQmax, U, A, eta, fx, fx0, x, x0, fx00, x00, p0, t0 As Double
            Dim nseg As Double
            Dim segmento As New PipeSection
            Dim results As New PipeResults

            Tspec = Me.OutletTemperature
            Pspec = Me.OutletPressure

            Dim countext As Integer = 0

            Do

                oms = Me.GetInletMaterialStream(0).Clone()
                oms.SetFlowsheet(Me.FlowSheet)
                oms.PreferredFlashAlgorithmTag = Me.PreferredFlashAlgorithmTag
                Me.PropertyPackage.CurrentMaterialStream = oms

                oms.Validate()

                'Iteracao para cada segmento
                Dim count As Integer = 0

                Dim currL As Double = 0.0#

                Dim j As Integer = 0

                With oms

                    Tin = .Phases(0).Properties.temperature.GetValueOrDefault
                    Pin = .Phases(0).Properties.pressure.GetValueOrDefault
                    Win = .Phases(0).Properties.massflow.GetValueOrDefault
                    Qin = .Phases(0).Properties.volumetric_flow.GetValueOrDefault
                    Hin = .Phases(0).Properties.enthalpy.GetValueOrDefault
                    Hout = Hin
                    Tout = Tin
                    Pout = Pin
                    TinP = Tin
                    PinP = Pin
                    HinP = Hin

                End With

                Dim tseg As Integer = 0
                For Each segmento In Me.Profile.Sections.Values
                    tseg += segmento.Incrementos
                Next

                For Each segmento In Me.Profile.Sections.Values

                    segmento.Resultados.Clear()

                    If segmento.TipoSegmento = "Tubulaosimples" Or segmento.TipoSegmento = "" Or segmento.TipoSegmento = "Straight Tube Section" Then

                        j = 0
                        nseg = segmento.Incrementos

                        With oms

                            w = .Phases(0).Properties.massflow.GetValueOrDefault
                            Tin = .Phases(0).Properties.temperature.GetValueOrDefault
                            Qlin = .Phases(3).Properties.volumetric_flow.GetValueOrDefault + .Phases(4).Properties.volumetric_flow.GetValueOrDefault + .Phases(5).Properties.volumetric_flow.GetValueOrDefault + .Phases(6).Properties.volumetric_flow.GetValueOrDefault
                            rho_l = .Phases(1).Properties.density.GetValueOrDefault
                            If Double.IsNaN(rho_l) Then rho_l = 0.0#
                            eta_l = .Phases(1).Properties.viscosity.GetValueOrDefault
                            K_l = .Phases(1).Properties.thermalConductivity.GetValueOrDefault
                            Cp_l = .Phases(1).Properties.heatCapacityCp.GetValueOrDefault
                            tens = .Phases(0).Properties.surfaceTension.GetValueOrDefault
                            If Double.IsNaN(tens) Then tens = 0.0#
                            w_l = .Phases(1).Properties.massflow.GetValueOrDefault

                            Qvin = .Phases(2).Properties.volumetric_flow.GetValueOrDefault
                            rho_v = .Phases(2).Properties.density.GetValueOrDefault
                            eta_v = .Phases(2).Properties.viscosity.GetValueOrDefault
                            K_v = .Phases(2).Properties.thermalConductivity.GetValueOrDefault
                            Cp_v = .Phases(2).Properties.heatCapacityCp.GetValueOrDefault
                            w_v = .Phases(2).Properties.massflow.GetValueOrDefault
                            z = .Phases(2).Properties.compressibilityFactor.GetValueOrDefault

                        End With

                        Do

                            If Text > Tin Then
                                Tout = Tin * 1.005
                            Else
                                Tout = Tin / 1.005
                            End If

                            If Tin < Text And Tout > Text Then Tout = Text * 0.98 + dText_dL * currL
                            If Tin > Text And Tout < Text Then Tout = Text * 1.02 + dText_dL * currL

                            cntT = 0
                            'Loop externo (convergencia do Delta T)
                            Do

                                cntP = 0
                                'Loop interno (convergencia do Delta P)
                                Do

                                    With segmento
                                        count = 0
                                        With results

                                            .TemperaturaInicial = Tin
                                            .PressaoInicial = Pin
                                            .EnergyFlow_Inicial = Hin
                                            .Cpl = Cp_l
                                            .Cpv = Cp_v
                                            .Kl = K_l
                                            .Kv = K_v
                                            .RHOl = rho_l
                                            .RHOv = rho_v
                                            .Ql = Qlin
                                            .Qv = Qvin
                                            .MUl = eta_l
                                            .MUv = eta_v
                                            .Surft = tens
                                            .LiqRe = 4 / Math.PI * .RHOl * .Ql / (.MUl * segmento.DI * 0.0254)
                                            .VapRe = 4 / Math.PI * .RHOv * .Qv / (.MUv * segmento.DI * 0.0254)
                                            .LiqVel = .Ql / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)
                                            .VapVel = .Qv / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)

                                        End With

                                        resv = fpp.CalculateDeltaP(.DI * 0.0254, .Comprimento / .Incrementos, .Elevacao / .Incrementos, Me.rugosidade(.Material), Qvin * 24 * 3600, Qlin * 24 * 3600, eta_v * 1000, eta_l * 1000, rho_v, rho_l, tens)

                                        tipofluxo = resv(0)
                                        holdup = resv(1)
                                        dpf = resv(2)
                                        dph = resv(3)
                                        dpt = resv(4)

                                    End With

                                    Pout_ant2 = Pout_ant
                                    Pout_ant = Pout
                                    Pout = Pin - dpt

                                    fP_ant2 = fP_ant
                                    fP_ant = fP
                                    fP = Pout - Pout_ant

                                    If cntP > 3 Then
                                        Pout = Pout - fP * (Pout - Pout_ant2) / (fP - fP_ant2)
                                    End If

                                    cntP += 1

                                    If Pout <= 0 Then Throw New Exception(FlowSheet.GetTranslatedString("Pressonegativadentro"))

                                    If Double.IsNaN(Pout) Then Throw New Exception(FlowSheet.GetTranslatedString("Erronoclculodapresso"))

                                    If cntP > Me.MaxPressureIterations Then Throw New Exception(FlowSheet.GetTranslatedString("Ocalculadorexcedeuon"))

                                    FlowSheet.CheckStatus()

                                Loop Until Math.Abs(fP) < Me.TolP

                                With segmento

                                    Cp_m = holdup * Cp_l + (1 - holdup) * Cp_v

                                    If Not Me.ThermalProfile.TipoPerfil = ThermalEditorDefinitions.ThermalProfileType.Definir_Q Then
                                        If Me.ThermalProfile.TipoPerfil = ThermalEditorDefinitions.ThermalProfileType.Definir_CGTC Then
                                            U = Me.ThermalProfile.CGTC_Definido
                                            A = Math.PI * (.DE * 0.0254) * .Comprimento / .Incrementos
                                        ElseIf Me.ThermalProfile.TipoPerfil = ThermalEditorDefinitions.ThermalProfileType.Estimar_CGTC Then
                                            A = Math.PI * (.DE * 0.0254) * .Comprimento / .Incrementos
                                            Tpe = Tin + (Tout - Tin) / 2
                                            Dim resultU As Double() = CalcOverallHeatTransferCoefficient(.Material, holdup, .Comprimento / .Incrementos, _
                                                                                .DI * 0.0254, .DE * 0.0254, Me.rugosidade(.Material), Tpe, Text + dText_dL * currL,
                                                                                results.VapVel, results.LiqVel, results.Cpl, results.Cpv, results.Kl, results.Kv, _
                                                                                results.MUl, results.MUv, results.RHOl, results.RHOv, _
                                                                                Me.ThermalProfile.Incluir_cti, Me.ThermalProfile.Incluir_isolamento, _
                                                                                Me.ThermalProfile.Incluir_paredes, Me.ThermalProfile.Incluir_cte)
                                            U = resultU(0)
                                            With results
                                                .HTC_internal = resultU(1)
                                                .HTC_pipewall = resultU(2)
                                                .HTC_insulation = resultU(3)
                                                .HTC_external = resultU(4)
                                            End With
                                        End If
                                        If U <> 0.0# Then
                                            DQ = (Tout - Tin) / Math.Log((Text + dText_dL * currL - Tin) / (Text + dText_dL * currL - Tout)) * U / 1000 * A
                                            DQmax = (Text + dText_dL * currL - Tin) * Cp_m * Win
                                            If Double.IsNaN(DQ) Then DQ = 0.0#
                                            If Math.Abs(DQ) > Math.Abs(DQmax) Then DQ = DQmax
                                            'Tout = DQ / (Win * Cp_m) + Tin
                                        Else
                                            'Tout = Tin
                                            DQ = 0.0#
                                            DQmax = 0.0#
                                        End If
                                    Else
                                        DQ = Me.ThermalProfile.Calor_trocado / tseg
                                        'Tout = DQ / (Win * Cp_m) + Tin
                                        A = Math.PI * (.DE * 0.0254) * .Comprimento / .Incrementos
                                        U = DQ / (A * (Tout - Tin)) * 1000
                                    End If
                                End With

                                Hout = Hin + DQ / Win

                                oms.PropertyPackage.CurrentMaterialStream = oms

                                Tout_ant = Tout
                                Tout = oms.PropertyPackage.FlashBase.CalculateEquilibrium(PropertyPackages.FlashSpec.P, PropertyPackages.FlashSpec.H, Pout, Hout, oms.PropertyPackage, oms.PropertyPackage.RET_VMOL(PropertyPackages.Phase.Mixture), Nothing, Tout).CalculatedTemperature
                                Tout = 0.7 * Tout_ant + 0.3 * Tout

                                fT = Tout - Tout_ant

                                If Math.Abs(fT) < Me.TolT Then Exit Do

                                cntT += 1

                                If Tout <= 0 Or Double.IsNaN(Tout) Then
                                    Throw New Exception(FlowSheet.GetTranslatedString("Erronoclculodatemper"))
                                End If

                                If cntT > Me.MaxTemperatureIterations Then Throw New Exception(FlowSheet.GetTranslatedString("Ocalculadorexcedeuon1"))

                                FlowSheet.CheckStatus()

                            Loop

                            If IncludeJTEffect Then

                                Cp_m = (w_l * Cp_l + w_v * Cp_v) / w

                                If oms.Phases(2).Properties.molarfraction.GetValueOrDefault > 0 Then
                                    oms.Phases(0).Properties.temperature = Tin - 2
                                    oms.PropertyPackage.CurrentMaterialStream = oms
                                    oms.PropertyPackage.DW_CalcPhaseProps(PropertyPackages.Phase.Vapor)
                                    z2 = oms.Phases(2).Properties.compressibilityFactor.GetValueOrDefault
                                    dzdT = (z2 - z) / -2
                                Else
                                    dzdT = 0.0#
                                End If

                                If w_l <> 0.0# Then
                                    eta = 1 / (Cp_m * w) * (w_v / rho_v * (-Tin / z * dzdT) + w_l / rho_l)
                                Else
                                    eta = 1 / (Cp_m * w) * (w_v / rho_v * (-Tin / z * dzdT))
                                End If

                                Hout = Hout - eta * Cp_m * (Pout - Pin) / w

                                Toutj = Tout + eta * (Pin - Pout) / 1000

                                Tout_ant = Tout
                                Tout = Toutj

                            End If

                            oms.PropertyPackage.CurrentMaterialStream = oms

                            oms.Phases(0).Properties.temperature = Tout
                            oms.Phases(0).Properties.pressure = Pout
                            oms.Phases(0).Properties.enthalpy = Hout

                            oms.SpecType = Interfaces.Enums.StreamSpec.Pressure_and_Enthalpy

                            oms.Calculate(True, True)

                            With oms

                                w = .Phases(0).Properties.massflow.GetValueOrDefault
                                Hout = .Phases(0).Properties.enthalpy.GetValueOrDefault
                                Tout = .Phases(0).Properties.temperature.GetValueOrDefault

                                Qlin = .Phases(3).Properties.volumetric_flow.GetValueOrDefault + .Phases(4).Properties.volumetric_flow.GetValueOrDefault + .Phases(5).Properties.volumetric_flow.GetValueOrDefault + .Phases(6).Properties.volumetric_flow.GetValueOrDefault
                                rho_l = .Phases(1).Properties.density.GetValueOrDefault
                                If Double.IsNaN(rho_l) Then rho_l = 0.0#
                                eta_l = .Phases(1).Properties.viscosity.GetValueOrDefault
                                K_l = .Phases(1).Properties.thermalConductivity.GetValueOrDefault
                                Cp_l = .Phases(1).Properties.heatCapacityCp.GetValueOrDefault
                                tens = .Phases(0).Properties.surfaceTension.GetValueOrDefault
                                If Double.IsNaN(tens) Then rho_l = 0.0#
                                w_l = .Phases(1).Properties.massflow.GetValueOrDefault

                                Qvin = .Phases(2).Properties.volumetric_flow.GetValueOrDefault
                                rho_v = .Phases(2).Properties.density.GetValueOrDefault
                                eta_v = .Phases(2).Properties.viscosity.GetValueOrDefault
                                K_v = .Phases(2).Properties.thermalConductivity.GetValueOrDefault
                                Cp_v = .Phases(2).Properties.heatCapacityCp.GetValueOrDefault
                                w_v = .Phases(2).Properties.massflow.GetValueOrDefault
                                z = .Phases(2).Properties.compressibilityFactor.GetValueOrDefault

                            End With

                            With results

                                .CalorTransferido = DQ
                                .DpPorFriccao = dpf
                                .DpPorHidrostatico = dph
                                .HoldupDeLiquido = holdup
                                .TipoFluxo = tipofluxo

                                segmento.Resultados.Add(New PipeResults(.PressaoInicial, .TemperaturaInicial, .MUv, .MUl, .RHOv, .RHOl,
                                                                        .Cpv, .Cpl, .Kv, .Kl, .Qv, .Ql, .Surft, .DpPorFriccao, .DpPorHidrostatico,
                                                                        .HoldupDeLiquido, .TipoFluxo, .LiqRe, .VapRe, .LiqVel, .VapVel, .CalorTransferido,
                                                                        .EnergyFlow_Inicial, U) With {.HTC_external = results.HTC_external,
                                                                                                   .HTC_internal = results.HTC_internal,
                                                                                                   .HTC_insulation = results.HTC_insulation,
                                                                                                   .HTC_pipewall = results.HTC_pipewall})

                            End With

                            Hin = Hout
                            Tin = Tout
                            Pin = Pout

                            j += 1

                        Loop Until j = nseg

                    Else

                        'CALCULAR DP PARA VALVULAS
                        count = 0

                        If segmento.Indice = 1 Then

                            With Me.PropertyPackage

                                Tpe = Tin + (Tout - Tin) / 2
                                Tpe = Tin + (Tout - Tin) / 2
                                Ppe = Pin + (Pout - Pin) / 2

                                oms.Phases(0).Properties.temperature = Tpe
                                oms.Phases(0).Properties.pressure = Ppe

                                oms.Calculate(True, True)

                            End With

                            With oms

                                Qlin = .Phases(3).Properties.volumetric_flow.GetValueOrDefault + .Phases(4).Properties.volumetric_flow.GetValueOrDefault + .Phases(5).Properties.volumetric_flow.GetValueOrDefault + .Phases(6).Properties.volumetric_flow.GetValueOrDefault
                                rho_l = .Phases(1).Properties.density.GetValueOrDefault
                                eta_l = .Phases(1).Properties.viscosity.GetValueOrDefault
                                K_l = .Phases(1).Properties.thermalConductivity.GetValueOrDefault
                                Cp_l = .Phases(1).Properties.heatCapacityCp.GetValueOrDefault
                                tens = .Phases(0).Properties.surfaceTension.GetValueOrDefault

                                Qvin = .Phases(2).Properties.volumetric_flow.GetValueOrDefault
                                rho_v = .Phases(2).Properties.density.GetValueOrDefault
                                eta_v = .Phases(2).Properties.viscosity.GetValueOrDefault
                                K_v = .Phases(2).Properties.thermalConductivity.GetValueOrDefault
                                Cp_v = .Phases(2).Properties.heatCapacityCp.GetValueOrDefault

                            End With

                        End If

                        With segmento

                            count = 0

                            With results

                                .TemperaturaInicial = Tin
                                .PressaoInicial = Pin
                                .EnergyFlow_Inicial = Hin
                                .Cpl = Cp_l
                                .Cpv = Cp_v
                                .Kl = K_l
                                .Kv = K_v
                                .RHOl = rho_l
                                .RHOv = rho_v
                                .Ql = Qlin
                                .Qv = Qvin
                                .MUl = eta_l
                                .MUv = eta_v
                                .Surft = tens
                                .LiqRe = 4 / Math.PI * .RHOl * .Ql / (.MUl * segmento.DI * 0.0254)
                                .VapRe = 4 / Math.PI * .RHOv * .Qv / (.MUv * segmento.DI * 0.0254)
                                .LiqVel = .Ql / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)
                                .VapVel = .Qv / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)

                            End With

                            results.TipoFluxo = ""
                            resv = Me.Kfit(segmento.TipoSegmento)
                            If resv(1) Then
                                dph = 0
                                dpf = resv(0) * (0.0101 * (.DI * 0.0254) ^ -0.2232) * (Qlin / (Qvin + Qlin) * rho_l + Qvin / (Qvin + Qlin) * rho_v) * (results.LiqVel.GetValueOrDefault + results.VapVel.GetValueOrDefault) ^ 2 / 2
                            Else
                                dph = 0
                                dpf = resv(0) * (Qlin / (Qvin + Qlin) * rho_l + Qvin / (Qvin + Qlin) * rho_v) * (results.LiqVel.GetValueOrDefault + results.VapVel.GetValueOrDefault) ^ 2 / 2
                            End If
                            dpt = dpf

                            Pout = Pin - dpt
                            Tout = Tin
                            DQ = 0

                            With results

                                .CalorTransferido = DQ
                                .DpPorFriccao = dpf
                                .DpPorHidrostatico = dph
                                .HoldupDeLiquido = holdup
                                .TipoFluxo = FlowSheet.GetTranslatedString("Turbulento")
                                .TipoFluxoDescricao = ""

                                segmento.Resultados.Add(New PipeResults(.PressaoInicial, .TemperaturaInicial, .MUv, .MUl, .RHOv, .RHOl,
                                                                        .Cpv, .Cpl, .Kv, .Kl, .Qv, .Ql, .Surft, .DpPorFriccao, .DpPorHidrostatico,
                                                                        .HoldupDeLiquido, .TipoFluxo, .LiqRe, .VapRe, .LiqVel,
                                                                        .VapVel, .CalorTransferido, .EnergyFlow_Inicial, U))

                            End With

                            Hin = Hout
                            Tin = Tout
                            Pin = Pout

                        End With

                    End If

                    currL += segmento.Comprimento

                Next

                If Me.Specification = specmode.OutletTemperature Then
                    If Math.Abs(Tout - OutletTemperature) < 0.01 Then
                        Exit Do
                    Else
                        x00 = x0
                        x0 = x
                        x = Me.ThermalProfile.Calor_trocado
                        fx00 = fx0
                        fx0 = t0 - OutletTemperature
                        fx = Tout - OutletTemperature
                        If countext > 2 Then
                            x = x - fx * (x - x00) / (fx - fx00)
                            If Double.IsNaN(x) Or Double.IsInfinity(x) Then Throw New Exception(FlowSheet.GetTranslatedString("Erroaocalculartemper"))
                            Me.ThermalProfile.Calor_trocado = x
                        Else
                            Me.ThermalProfile.Calor_trocado += 0.1
                        End If
                    End If
                ElseIf Me.Specification = specmode.OutletPressure Then
                    If Math.Abs(Pout - OutletPressure) < 10 Then
                        Exit Do
                    Else
                        x00 = x0
                        x0 = x
                        x = Me.Profile.Sections(1).Comprimento
                        fx00 = fx0
                        fx0 = p0 - OutletPressure
                        fx = Pout - OutletPressure
                        If countext > 2 Then
                            x = x - fx * (x - x00) / (fx - fx00)
                            If Double.IsNaN(x) Or Double.IsInfinity(x) Then Throw New Exception(FlowSheet.GetTranslatedString("Erronoclculodapresso"))
                            Me.Profile.Sections(1).Comprimento = x
                        Else
                            Me.Profile.Sections(1).Comprimento *= 1.05
                        End If
                    End If
                Else
                    Exit Do
                End If

                p0 = Pout
                t0 = Tout

                countext += 1

                If countext > 50 Then Throw New Exception("Nmeromximodeiteraesa3")

            Loop

            CheckSpec(Tout, True, "outlet temperature")
            CheckSpec(Pout, True, "outlet pressure")
            CheckSpec(Hout, False, "outlet enthalpy")

            With results
                .TemperaturaInicial = Tout
                .PressaoInicial = Pout
                .EnergyFlow_Inicial = Hout
                .Cpl = Cp_l
                .Cpv = Cp_v
                .Kl = K_l
                .Kv = K_v
                .RHOl = rho_l
                .RHOv = rho_v
                .Ql = Qlin
                .Qv = Qvin
                .MUl = eta_l
                .MUv = eta_v
                .Surft = tens
                .LiqRe = 4 / Math.PI * .RHOl * .Ql / (.MUl * segmento.DI * 0.0254)
                .VapRe = 4 / Math.PI * .RHOv * .Qv / (.MUv * segmento.DI * 0.0254)
                .LiqVel = .Ql / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)
                .VapVel = .Qv / (Math.PI * (segmento.DI * 0.0254) ^ 2 / 4)
                .CalorTransferido = DQ
                .DpPorFriccao = dpf
                .DpPorHidrostatico = dph
                .HoldupDeLiquido = holdup
                .TipoFluxo = "-"
                .TipoFluxoDescricao = ""
                .HTC = U
            End With
            segmento.Resultados.Add(results)

            Me.DeltaP = (PinP - Pout)
            Me.DeltaT = (TinP - Tout)
            Me.DeltaQ = -(HinP - Hout) * Win

            'Atribuir valores a corrente de materia conectada a jusante
            With Me.GetOutletMaterialStream(0)
                .Phases(0).Properties.temperature = Tout
                .Phases(0).Properties.pressure = Pout
                .Phases(0).Properties.enthalpy = Hout
                Dim comp As BaseClasses.Compound
                For Each comp In .Phases(0).Compounds.Values
                    comp.MoleFraction = Me.GetInletMaterialStream(0).Phases(0).Compounds(comp.Name).MoleFraction
                    comp.MassFraction = Me.GetInletMaterialStream(0).Phases(0).Compounds(comp.Name).MassFraction
                Next
                .Phases(0).Properties.massflow = Me.GetInletMaterialStream(0).Phases(0).Properties.massflow.GetValueOrDefault
            End With

            'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
            With Me.GetEnergyStream
                .EnergyFlow = -Me.DeltaQ.Value
                .GraphicObject.Calculated = True
            End With

            segmento = Nothing
            results = Nothing

        End Sub

        Public Overrides Sub DeCalculate()

            Dim segmento As New PipeSection

            For Each segmento In Me.Profile.Sections.Values
                segmento.Resultados.Clear()
            Next

            'Zerar valores da corrente de materia conectada a jusante
            If Me.GraphicObject.OutputConnectors(0).IsAttached Then
                With Me.GetOutletMaterialStream(0)
                    .Phases(0).Properties.temperature = Nothing
                    .Phases(0).Properties.pressure = Nothing
                    .Phases(0).Properties.enthalpy = Nothing
                    .Phases(0).Properties.molarfraction = 1
                    .Phases(0).Properties.massfraction = 1
                    Dim comp As BaseClasses.Compound
                    Dim i As Integer = 0
                    For Each comp In .Phases(0).Compounds.Values
                        comp.MoleFraction = 0
                        comp.MassFraction = 0
                        i += 1
                    Next
                    .Phases(0).Properties.massflow = Nothing
                    .Phases(0).Properties.molarflow = Nothing
                    .GraphicObject.Calculated = False
                End With
            End If

            'Corrente de EnergyFlow - atualizar valor da potencia (kJ/s)
            If Me.GraphicObject.EnergyConnector.IsAttached Then
                With Me.GetEnergyStream
                    .EnergyFlow = Nothing
                    .GraphicObject.Calculated = False
                End With
            End If

            segmento = Nothing

        End Sub

#Region "        Funcoes"

        Function Kfit(ByVal name2 As String) As Array

            Dim name As String = name2.Substring(name2.IndexOf("[") + 1, name2.Length - name2.IndexOf("[") - 2)

            Dim tmp(1) As Double

            'Curva Normal 90°;30,00;1;
            If name = 0 Then
                tmp(0) = 30
                tmp(1) = 1
            End If
            'Curva Normal 45°;16,00;1;
            If name = 1 Then
                tmp(0) = 16
                tmp(1) = 1
            End If
            'Curva Normal 180°;50,00;1;
            If name = 2 Then
                tmp(0) = 50
                tmp(1) = 1
            End If
            'Valvula Angular;55,00;1;
            If name = 3 Then
                tmp(0) = 55
                tmp(1) = 1
            End If
            'Valvula Borboleta (2" a 14");40,00;1;
            If name = 4 Then
                tmp(0) = 40
                tmp(1) = 1
            End If
            'Valvula Esfera;3,00;1;
            If name = 5 Then
                tmp(0) = 3
                tmp(1) = 1
            End If
            'Valvula Gaveta (Aberta);8,00;1;
            If name = 6 Then
                tmp(0) = 8
                tmp(1) = 1
            End If
            'Valvula Globo;340,00;1;
            If name = 7 Then
                tmp(0) = 340
                tmp(1) = 1
            End If
            'Valvula Lift-Check;600,00;1;
            If name = 8 Then
                tmp(0) = 600
                tmp(1) = 1
            End If
            'Valvula Pe (Poppet Disc);420,00;1;
            If name = 9 Then
                tmp(0) = 420
                tmp(1) = 1
            End If
            'Valvula Retencao de Portinhola;100,00;1;
            If name = 10 Then
                tmp(0) = 100
                tmp(1) = 1
            End If
            'Valvula Stop-Check (Globo);400,00;1;
            If name = 11 Then
                tmp(0) = 400
                tmp(1) = 1
            End If
            'Te (saida bilateral);20,00;1;
            If name = 12 Then
                tmp(0) = 20
                tmp(1) = 1
            End If
            'Te (saida de lado);60,00;1;
            If name = 13 Then
                tmp(0) = 60
                tmp(1) = 1
            End If
            'Contracao Rapida d/D = 1/2;9,60;0;
            If name = 14 Then
                tmp(0) = 9.6
                tmp(1) = 0
            End If
            'Contracao Rapida d/D = 1/4;96,00;0;
            If name = 15 Then
                tmp(0) = 96
                tmp(1) = 0
            End If
            'Contracao Rapida d/D = 3/4;1,11;0;
            If name = 16 Then
                tmp(0) = 11
                tmp(1) = 0
            End If
            'Entrada Borda;0,25;0;
            If name = 17 Then
                tmp(0) = 0.25
                tmp(1) = 0
            End If
            'Entrada Normal;0,78;0;
            If name = 18 Then
                tmp(0) = 0.78
                tmp(1) = 0
            End If
            'Expansao Rapida d/D = 1/2;9,00;0;
            If name = 19 Then
                tmp(0) = 9
                tmp(1) = 0
            End If
            'Expansao Rapida d/D = 1/4;225,00;0;
            If name = 20 Then
                tmp(0) = 225
                tmp(1) = 0
            End If
            'Expansao Rapida d/D = 3/4;0,60;0;
            If name = 21 Then
                tmp(0) = 0.6
                tmp(1) = 0
            End If
            'Joelho em 90°;60,00;1;
            If name = 22 Then
                tmp(0) = 60
                tmp(1) = 1
            End If
            'Reducao Normal 2:1;5,67;0;
            If name = 23 Then
                tmp(0) = 5.67
                tmp(1) = 0
            End If
            'Reducao Normal 4:3;0,65;0;
            If name = 24 Then
                tmp(0) = 0.65
                tmp(1) = 0
            End If
            'Saida Borda;1,00;0;
            If name = 25 Then
                tmp(0) = 1
                tmp(1) = 0
            End If
            'Saida Normal;1,00;0;
            If name = 26 Then
                tmp(0) = 1
                tmp(1) = 0
            End If

            Kfit = tmp

        End Function

        Function cond_isol(ByVal meio As Integer) As Double

            'Asfalto
            'Concreto
            'Espuma de Poliuretano
            'Espuma de PVC
            'Fibra de vidro
            'Plastico
            'Vidro
            'Definido pelo usuario

            cond_isol = 0

            If meio = 0 Then

                cond_isol = 0.7

            ElseIf meio = 1 Then

                cond_isol = 1

            ElseIf meio = 2 Then

                cond_isol = 0.018

            ElseIf meio = 3 Then

                cond_isol = 0.04

            ElseIf meio = 4 Then

                cond_isol = 0.035

            ElseIf meio = 5 Then

                cond_isol = 0.036

            ElseIf meio = 6 Then

                cond_isol = 0.08

            ElseIf meio = 7 Then

                cond_isol = 0

            End If

            'condutividade em W/(m.K)

        End Function

        Function rugosidade(ByVal material As String) As Double

            Dim epsilon As Double
            'rugosidade em metros

            If material = FlowSheet.GetTranslatedString("AoComum") Then epsilon = 0.0000457
            If material = FlowSheet.GetTranslatedString("AoCarbono") Then epsilon = 0.000045
            If material = FlowSheet.GetTranslatedString("FerroBottomido") Then epsilon = 0.000259
            If material = FlowSheet.GetTranslatedString("AoInoxidvel") Then epsilon = 0.000045
            If material = "PVC" Then epsilon = 0.0000015
            If material = "PVC+PFRV" Then epsilon = 0.0000015
            If material = FlowSheet.GetTranslatedString("CommercialCopper") Then epsilon = 0.0000015

            rugosidade = epsilon

        End Function

        Function k_parede(ByVal material As String, ByVal T As Double) As Double

            Dim kp As Double
            'condutividade termica da parede do duto, em W/m.K

            If material = FlowSheet.GetTranslatedString("AoComum") Then kp = -0.000000004 * T ^ 3 - 0.00002 * T ^ 2 + 0.021 * T + 33.743
            If material = FlowSheet.GetTranslatedString("AoCarbono") Then kp = 0.000000007 * T ^ 3 - 0.00002 * T ^ 2 - 0.0291 * T + 70.765
            If material = FlowSheet.GetTranslatedString("FerroBottomido") Then kp = -0.00000008 * T ^ 3 + 0.0002 * T ^ 2 - 0.211 * T + 127.99
            If material = FlowSheet.GetTranslatedString("AoInoxidvel") Then kp = 14.6 + 0.0127 * (T - 273.15)
            If material = "PVC" Then kp = 0.16
            If material = "PVC+PFRV" Then kp = 0.16
            If material = FlowSheet.GetTranslatedString("CommercialCopper") Then kp = 420.75 - 0.068493 * T

            k_parede = kp   'W/m.K

        End Function

        Function k_terreno(ByVal terreno As Integer) As Double

            Dim kt = 0.0#

            If terreno = 2 Then kt = 1.1
            If terreno = 3 Then kt = 1.95
            If terreno = 4 Then kt = 0.5
            If terreno = 5 Then kt = 2.2

            k_terreno = kt

        End Function

        Function CalcOverallHeatTransferCoefficient(ByVal materialparede As String, ByVal EL As Double, ByVal L As Double, _
                            ByVal Dint As Double, ByVal Dext As Double, ByVal rugosidade As Double, _
                            ByVal T As Double, ByVal Text As Double, ByVal vel_g As Double, ByVal vel_l As Double, _
                            ByVal Cpl As Double, ByVal Cpv As Double, ByVal kl As Double, ByVal kv As Double, _
                            ByVal mu_l As Double, ByVal mu_v As Double, ByVal rho_l As Double, _
                            ByVal rho_v As Double, ByVal hinterno As Boolean, ByVal isolamento As Boolean, _
                            ByVal parede As Boolean, ByVal hexterno As Boolean) As Double()

            If Double.IsNaN(rho_l) Then rho_l = 0.0#

            'Calculate average properties
            Dim vel As Double = vel_g + vel_l 'm/s
            Dim mu As Double = EL * mu_l + (1 - EL) * mu_v 'Pa.s
            Dim rho As Double = EL * rho_l + (1 - EL) * rho_v 'kg/m3
            Dim Cp As Double = 1000 * (EL * Cpl + (1 - EL) * Cpv) 'J/kg.K
            Dim k As Double = EL * kl + (1 - EL) * kv 'W/[m.K]
            Dim Cpmist = Cp

            'Internal HTC calculation
            Dim U_int As Double

            If hinterno Then

                'Internal Re calc
                Dim Re_int = NRe(rho, vel, Dint, mu)

                Dim epsilon = Me.rugosidade(materialparede)
                Dim ffint = 0.0#
                If Re_int > 3250 Then
                    Dim a1 = Math.Log(((epsilon / Dint) ^ 1.1096) / 2.8257 + (7.149 / Re_int) ^ 0.8961) / Math.Log(10.0#)
                    Dim b1 = -2 * Math.Log((epsilon / Dint) / 3.7065 - 5.0452 * a1 / Re_int) / Math.Log(10.0#)
                    ffint = (1 / b1) ^ 2
                Else
                    ffint = 64 / Re_int
                End If

                'Internal Pr calc
                Dim Pr_int = NPr(Cp, mu, k)

                'Internal h calc
                Dim h_int = hint_petukhov(k, Dint, ffint, Re_int, Pr_int)

                'Internal h contribution
                U_int = h_int

            End If

            'Pipe wall HTC contribution
            Dim U_parede = 0.0#

            If parede = True Then

                U_parede = k_parede(materialparede, T) / (Math.Log(Dext / Dint) * Dint)
                If Dext = Dint Then U_parede = 0.0#

            End If

            'Insulation HTC contribution
            Dim U_isol = 0.0#

            Dim esp_isol = 0.0#
            If isolamento = True Then

                esp_isol = Me.m_thermalprofile.Espessura
                U_isol = Me.m_thermalprofile.Condtermica / (Math.Log((Dext + esp_isol) / Dext) * Dext)

            End If

            'External HTC contribution
            Dim U_ext = 0.0#

            If hexterno = True Then

                Dim mu2, k2, cp2, rho2 As Double 'Soil, undergound

                If Me.m_thermalprofile.Meio <> "0" And Me.m_thermalprofile.Meio <> "1" Then

                    Dim Zb = Convert.ToDouble(Me.m_thermalprofile.Velocidade)

                    Dim Rs = (Dext + esp_isol) / (2 * k_terreno(Me.m_thermalprofile.Meio)) * Math.Log((2 * Zb + (4 * Zb ^ 2 - (Dext + esp_isol) ^ 2) ^ 0.5) / (Dext + esp_isol))

                    If Zb > 0 Then
                        U_ext = 1 / Rs
                    Else
                        U_ext = 1000000.0
                    End If

                ElseIf Me.m_thermalprofile.Meio = "0" Then 'Air

                    'Average air properties
                    vel = Convert.ToDouble(Me.m_thermalprofile.Velocidade)
                    Dim props = PropsAR(Text, 101325)
                    mu2 = props(1)
                    rho2 = props(0)
                    cp2 = props(2) * 1000
                    k2 = props(3)

                    'External Re
                    Dim Re_ext = NRe(rho2, vel, (Dext + esp_isol), mu2)

                    'External Pr
                    Dim Pr_ext = NPr(cp2, mu2, k2)

                    'External h
                    Dim h_ext = hext_holman(k2, (Dext + esp_isol), Re_ext, Pr_ext)

                    'External HTC contribution
                    U_ext = h_ext * (Dext + esp_isol) / Dint

                ElseIf Me.m_thermalprofile.Meio = 1 Then 'Water

                    'Average water properties
                    vel = Convert.ToDouble(Me.m_thermalprofile.Velocidade)
                    Dim props = PropsAGUA(Text, 101325)
                    mu2 = props(1)
                    rho2 = props(0)
                    cp2 = props(2) * 1000
                    k2 = props(3)

                    'External Re
                    Dim Re_ext = NRe(rho2, vel, (Dext + esp_isol), mu2)

                    'External Pr
                    Dim Pr_ext = NPr(cp2, mu2, k2)

                    'External h
                    Dim h_ext = hext_holman(k2, (Dext + esp_isol), Re_ext, Pr_ext)

                    'External HTC contribution
                    U_ext = h_ext * (Dext + esp_isol) / Dint

                End If

            End If

            'Calculate overall HTC
            Dim _U As Double

            If U_int <> 0.0# Then
                _U = _U + 1 / U_int
            Else
                If hinterno = True Then
                    _U = _U + 1.0E+30
                End If
            End If
            If U_parede <> 0.0# Then
                _U = _U + 1 / U_parede
            Else
                If parede = True Then
                    _U = _U + 1.0E+30
                End If
            End If
            If U_isol <> 0.0# Then
                _U = _U + 1 / U_isol
            Else
                If isolamento = True Then
                    _U = _U + 1.0E+30
                End If
            End If
            If U_ext <> 0.0# Then
                _U = _U + 1 / U_ext
            Else
                If hexterno = True Then
                    _U = _U + 1.0E+30
                End If
            End If

            Return New Double() {1 / _U, U_int, U_parede, U_isol, U_ext} '[W/m².K]

        End Function

        Shared Function NRe(ByVal rho As Double, ByVal v As Double, ByVal D As Double, ByVal mu As Double) As Double

            NRe = rho * v * D / mu

        End Function

        Shared Function NPr(ByVal Cp As Double, ByVal mu As Double, ByVal k As Double) As Double

            NPr = Cp * mu / k

        End Function

        Shared Function hext_holman(ByVal k As Double, ByVal Dext As Double, ByVal NRe As Double, ByVal NPr As Double) As Double

            hext_holman = k / Dext * 0.25 * NRe ^ 0.6 * NPr ^ 0.38

        End Function

        Shared Function hint_petukhov(ByVal k, ByVal D, ByVal f, ByVal NRe, ByVal NPr)

            hint_petukhov = k / D * (f / 8) * NRe * NPr / (1.07 + 12.7 * (f / 8) ^ 0.5 * (NPr ^ (2 / 3) - 1))

        End Function

        Shared Function PropsAR(ByVal Tamb As Double, ByVal Pamb As Double)

            Dim T = Tamb

            Dim rho = 314.56 * T ^ -0.9812

            'viscosidade
            Dim mu = rho * (0.000001 * (0.00009 * T ^ 2 + 0.035 * T - 2.9346))

            'capacidade calorifica
            Dim Cp = 0.000000000001 * T ^ 4 - 0.000000003 * T ^ 3 + 0.000002 * T ^ 2 - 0.0008 * T + 1.091

            'condutividade termica
            Dim k = -0.00000002 * T ^ 2 + 0.00009 * T + 0.0012

            Dim tmp2(3)

            tmp2(0) = rho
            tmp2(1) = mu
            tmp2(2) = Cp
            tmp2(3) = k

            PropsAR = tmp2

        End Function

        Protected m_iapws97 As New IAPWS_IF97

        Function PropsAGUA(ByVal Tamb As Double, ByVal Pamb As Double)

            'massa molar
            Dim mm = 18
            Dim Tc = 647.3
            Dim Pc = 217.6 * 101325
            Dim Vc = 0.000001 * 56
            Dim Zc = 0.229
            Dim w = 0.344
            Dim ZRa = 0.237

            Dim R = 8.314
            Dim P = Pamb
            Dim T = Tamb

            'densidade
            Dim rho = Me.m_iapws97.densW(T, P / 100000)

            'viscosidade
            Dim mu = Me.m_iapws97.viscW(T, P / 100000)

            'capacidade calorifica
            Dim Cp = Me.m_iapws97.cpW(T, P / 100000)

            'condutividade termica
            Dim k = Me.m_iapws97.thconW(T, P / 100000)

            Dim tmp2(3)

            tmp2(0) = rho
            tmp2(1) = mu
            tmp2(2) = Cp
            tmp2(3) = k

            PropsAGUA = tmp2

        End Function

        Function CALCT2(ByVal U As Double, ByVal DQ As Double, ByVal T1 As Double, ByVal Tamb As Double) As Double

            Dim T, Tinf, Tsup As Double
            Dim fT, fT_inf, nsub, delta_T As Double

START_LOOP:

            If T1 > Tamb Then
                Tinf = Tamb
                Tsup = T1
            Else
                Tinf = T1
                Tsup = Tamb
            End If

            nsub = 5

            delta_T = (Tsup - Tinf) / nsub
            Dim idx As Integer = 0
            Do
                fT = Me.FT2(T1, Tinf, Tamb, U, DQ)
                Tinf = Tinf + delta_T
                fT_inf = Me.FT2(T1, Tinf, Tamb, U, DQ)
                idx += 1
                If Not Double.TryParse(Tinf, New Double) Or Double.IsNaN(Tinf) Or Tinf < 100 Or idx > 100 Then Throw New Exception("Erro ao calcular temperatura")
            Loop Until fT * fT_inf < 0 Or Tinf > Tsup
            'If Tinf > Tsup Then Throw New Exception(FlowSheet.GetTranslatedString("Erroaocalculartemper"))
            Tsup = Tinf
            Tinf = Tinf - delta_T

            'metodo de Brent para encontrar Vc

            Dim aaa, bbb, ccc, ddd, eee, min11, min22, faa, fbb, fcc, ppp, qqq, rrr, sss, tol11, xmm As Double
            Dim ITMAX2 As Integer = 10000
            Dim iter2 As Integer

            aaa = Tinf
            bbb = Tsup
            ccc = Tsup

            faa = Me.FT2(T1, Tinf, aaa, U, DQ)
            fbb = Me.FT2(T1, Tinf, bbb, U, DQ)
            fcc = fbb

            iter2 = 0
            Do
                If (fbb > 0 And fcc > 0) Or (fbb < 0 And fcc < 0) Then
                    ccc = aaa
                    fcc = faa
                    ddd = bbb - aaa
                    eee = ddd
                End If
                If Math.Abs(fcc) < Math.Abs(fbb) Then
                    aaa = bbb
                    bbb = ccc
                    ccc = aaa
                    faa = fbb
                    fbb = fcc
                    fcc = faa
                End If
                tol11 = 0.0000001
                xmm = 0.5 * (ccc - bbb)
                If (Math.Abs(xmm) <= tol11) Or (fbb = 0) Then GoTo Final3
                If (Math.Abs(eee) >= tol11) And (Math.Abs(faa) > Math.Abs(fbb)) Then
                    sss = fbb / faa
                    If aaa = ccc Then
                        ppp = 2 * xmm * sss
                        qqq = 1 - sss
                    Else
                        qqq = faa / fcc
                        rrr = fbb / fcc
                        ppp = sss * (2 * xmm * qqq * (qqq - rrr) - (bbb - aaa) * (rrr - 1))
                        qqq = (qqq - 1) * (rrr - 1) * (sss - 1)
                    End If
                    If ppp > 0 Then qqq = -qqq
                    ppp = Math.Abs(ppp)
                    min11 = 3 * xmm * qqq - Math.Abs(tol11 * qqq)
                    min22 = Math.Abs(eee * qqq)
                    Dim tvar2 As Double
                    If min11 < min22 Then tvar2 = min11
                    If min11 > min22 Then tvar2 = min22
                    If 2 * ppp < tvar2 Then
                        eee = ddd
                        ddd = ppp / qqq
                    Else
                        ddd = xmm
                        eee = ddd
                    End If
                Else
                    ddd = xmm
                    eee = ddd
                End If
                aaa = bbb
                faa = fbb
                If (Math.Abs(ddd) > tol11) Then
                    bbb += ddd
                Else
                    bbb += Math.Sign(xmm) * tol11
                End If
                fbb = Me.FT2(T1, bbb, Tamb, U, DQ)
                iter2 += 1
            Loop Until iter2 = ITMAX2

Final3:     T = bbb

            Return T

        End Function

        Function FT2(ByVal T1 As Double, ByVal T2 As Double, ByVal Tamb As Double, ByVal U As Double, ByVal DQ As Double) As Double

            Dim f As Double
            If T1 < Tamb Then
                f = U * (T1 - T2) / Math.Log((Tamb - T2) / (Tamb - T1)) - DQ
            Else
                f = U * (T2 - T1) / Math.Log((Tamb - T1) / (Tamb - T2)) - DQ
            End If

            'If Double.TryParse(f, New Double) Then
            '    Return f
            'Else
            '    Return Tamb
            'End If
            Return f

        End Function

#End Region

        Public Overrides Function GetPropertyValue(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Object

            Dim val0 As Object = MyBase.GetPropertyValue(prop, su)

            If su Is Nothing Then su = New SystemsOfUnits.SI

            If Not val0 Is Nothing Then

                Return val0

            ElseIf prop.Contains("_") Then

                Dim value As Double = 0
                Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

                Select Case propidx
                    Case 0
                        value = cv.ConvertFromSI(su.deltaP, Me.DeltaP.GetValueOrDefault)
                    Case 1
                        value = cv.ConvertFromSI(su.deltaT, Me.DeltaT.GetValueOrDefault)
                    Case 2
                        value = cv.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault)
                    Case 3
                        value = cv.ConvertFromSI(su.pressure, Me.OutletPressure)
                    Case 4
                        value = cv.ConvertFromSI(su.temperature, Me.OutletTemperature)
                    Case 5
                        value = cv.ConvertFromSI(su.heat_transf_coeff, Me.ThermalProfile.CGTC_Definido)
                    Case 6
                        value = cv.ConvertFromSI(su.temperature, Me.ThermalProfile.Temp_amb_definir)
                    Case 7
                        value = cv.ConvertFromSI(su.deltaT, Me.ThermalProfile.AmbientTemperatureGradient) / cv.ConvertFromSI(su.distance, 1.0#)
                End Select
                Return value
            Else
                Try
                    If prop.Contains("Results") Then
                        Dim skey As Integer = prop.Split(",")(1)
                        Dim sindex As Integer = prop.Split(",")(3) - 1
                        Dim sprop As String = prop.Split(",")(4)
                        Select Case sprop
                            Case "HeatTransfer"
                                Return cv.ConvertFromSI(su.heatflow, Profile.Sections(skey).Resultados(sindex).CalorTransferido)
                            Case "HeatCapacityLiquid"
                                Return cv.ConvertFromSI(su.heatCapacityCp, Profile.Sections(skey).Resultados(sindex).Cpl)
                            Case "HeatCapacityVapor"
                                Return cv.ConvertFromSI(su.heatCapacityCp, Profile.Sections(skey).Resultados(sindex).Cpv)
                            Case "PressureDropFriction"
                                Return cv.ConvertFromSI(su.deltaP, Profile.Sections(skey).Resultados(sindex).DpPorFriccao)
                            Case "PressureDropHydrostatic"
                                Return cv.ConvertFromSI(su.deltaP, Profile.Sections(skey).Resultados(sindex).DpPorHidrostatico)
                            Case "PressureDropTotal"
                                Return cv.ConvertFromSI(su.deltaP, Profile.Sections(skey).Resultados(sindex).DpPorFriccao + Profile.Sections(skey).Resultados(sindex).DpPorHidrostatico)
                            Case "LiquidHoldup"
                                Return Profile.Sections(skey).Resultados(sindex).HoldupDeLiquido
                            Case "HTCoverall"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, Profile.Sections(skey).Resultados(sindex).HTC)
                            Case "HTCexternal"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, Profile.Sections(skey).Resultados(sindex).HTC_external)
                            Case "HTCinternal"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, Profile.Sections(skey).Resultados(sindex).HTC_internal)
                            Case "HTCinsulation"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, Profile.Sections(skey).Resultados(sindex).HTC_insulation)
                            Case "HTCpipewall"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, Profile.Sections(skey).Resultados(sindex).HTC_pipewall)
                            Case "ThermalConductivityLiquid"
                                Return cv.ConvertFromSI(su.thermalConductivity, Profile.Sections(skey).Resultados(sindex).Kl)
                            Case "ThermalConductivityVapor"
                                Return cv.ConvertFromSI(su.thermalConductivity, Profile.Sections(skey).Resultados(sindex).Kv)
                            Case "ReynoldsNumberLiquid"
                                Return Profile.Sections(skey).Resultados(sindex).LiqRe
                            Case "ReynoldsNumberVapor"
                                Return Profile.Sections(skey).Resultados(sindex).VapRe
                            Case "ViscosityLiquid"
                                Return cv.ConvertFromSI(su.viscosity, Profile.Sections(skey).Resultados(sindex).MUl)
                            Case "ViscosityVapor"
                                Return cv.ConvertFromSI(su.viscosity, Profile.Sections(skey).Resultados(sindex).MUv)
                            Case "VolumetricFlowLiquid"
                                Return cv.ConvertFromSI(su.volumetricFlow, Profile.Sections(skey).Resultados(sindex).Ql)
                            Case "VolumetricFlowVapor"
                                Return cv.ConvertFromSI(su.volumetricFlow, Profile.Sections(skey).Resultados(sindex).Qv)
                            Case "DensityLiquid"
                                Return cv.ConvertFromSI(su.density, Profile.Sections(skey).Resultados(sindex).RHOl)
                            Case "DensityVapor"
                                Return cv.ConvertFromSI(su.density, Profile.Sections(skey).Resultados(sindex).RHOv)
                            Case "SurfaceTension"
                                Return cv.ConvertFromSI(su.surfaceTension, Profile.Sections(skey).Resultados(sindex).Surft)
                            Case "InitialTemperature"
                                Return cv.ConvertFromSI(su.temperature, Profile.Sections(skey).Resultados(sindex).TemperaturaInicial)
                            Case "FlowRegime"
                                Return Profile.Sections(skey).Resultados(sindex).TipoFluxo
                            Case "VelocityLiquid"
                                Return cv.ConvertFromSI(su.velocity, Profile.Sections(skey).Resultados(sindex).LiqVel)
                            Case "VelocityVapor"
                                Return cv.ConvertFromSI(su.velocity, Profile.Sections(skey).Resultados(sindex).VapVel)
                        End Select
                    ElseIf prop.Contains("HydraulicSegment") Then
                        Dim skey As Integer = prop.Split(",")(1)
                        Dim sprop As String = prop.Split(",")(2)
                        Select Case sprop
                            Case "Length"
                                Return cv.ConvertFromSI(su.distance, Profile.Sections(skey).Comprimento)
                            Case "Elevation"
                                Return cv.ConvertFromSI(su.distance, Profile.Sections(skey).Elevacao)
                            Case "InternalDiameter"
                                Return cv.Convert("in.", su.diameter, Profile.Sections(skey).DI)
                            Case "ExternalDiameter"
                                Return cv.Convert("in.", su.diameter, Profile.Sections(skey).DE)
                            Case "Sections"
                                Return Profile.Sections(skey).Incrementos
                            Case Else
                                Return 0.0
                        End Select
                    ElseIf prop.Contains("ThermalProfile") Then
                        Dim tprop As String = prop.Split(",")(1)
                        Select Case tprop
                            Case "CalculationType"
                                Return ThermalProfile.TipoPerfil
                            Case "OverallHTC"
                                Return cv.ConvertFromSI(su.heat_transf_coeff, ThermalProfile.CGTC_Definido)
                            Case "ExternalTemperatureDefinedHTC"
                                Return cv.ConvertFromSI(su.temperature, ThermalProfile.Temp_amb_definir)
                            Case "ExternalTemperatureEstimatedHTC"
                                Return cv.ConvertFromSI(su.temperature, ThermalProfile.Temp_amb_estimar)
                            Case "ExternalTemperatureGradientDefinedHTC"
                                Return cv.ConvertFromSI(su.deltaT, Me.ThermalProfile.AmbientTemperatureGradient) / cv.ConvertFromSI(su.distance, 1.0#)
                            Case "ExternalTemperatureGradientEstimatedHTC"
                                Return cv.ConvertFromSI(su.deltaT, Me.ThermalProfile.AmbientTemperatureGradient_EstimateHTC) / cv.ConvertFromSI(su.distance, 1.0#)
                            Case "HeatExchanged"
                                Return cv.ConvertFromSI(su.heatflow, ThermalProfile.Calor_trocado)
                            Case "IncludeWallHTC"
                                Return ThermalProfile.Incluir_paredes
                            Case "IncludeInternalHTC"
                                Return ThermalProfile.Incluir_cti
                            Case "IncludeInsulationHTC"
                                Return ThermalProfile.Incluir_isolamento
                            Case "InsulationThickness"
                                Return cv.ConvertFromSI(su.thickness, ThermalProfile.Espessura)
                            Case "InsulationThermalConductivity"
                                Return cv.ConvertFromSI(su.thermalConductivity, ThermalProfile.Condtermica)
                            Case "IncludeExternalHTC"
                                Return ThermalProfile.Incluir_cte
                            Case "ExternalEnvironmentType"
                                Return ThermalProfile.Meio
                            Case "ExternalEnvironmentVelocityOrDeepness"
                                Return cv.ConvertFromSI(su.velocity, ThermalProfile.Velocidade)
                            Case Else
                                Return 0.0
                        End Select
                    Else
                        Return 0.0#
                    End If
                Catch ex As Exception
                    Return "Error"
                End Try
            End If

        End Function

        Public Overrides Function GetDefaultProperties() As String()
            Return New String() {"PROP_PS_0", "PROP_PS_1", "PROP_PS_2"}
        End Function

        Public Overloads Overrides Function GetProperties(ByVal proptype As Interfaces.Enums.PropertyType) As String()
            Dim i As Integer = 0
            Dim proplist As New ArrayList
            Dim basecol = MyBase.GetProperties(proptype)
            If basecol.Length > 0 Then proplist.AddRange(basecol)
            For i = 0 To 7
                proplist.Add("PROP_PS_" + CStr(i))
            Next
            For Each ps In Profile.Sections
                proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Length")
                proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Elevation")
                proplist.Add("HydraulicSegment," + ps.Key.ToString + ",InternalDiameter")
                proplist.Add("HydraulicSegment," + ps.Key.ToString + ",ExternalDiameter")
                proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Sections")
            Next
            For Each ps In Profile.Sections
                Dim j As Integer = 1
                For Each res In ps.Value.Resultados
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HeatTransfer")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HeatCapacityLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HeatCapacityVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",PressureDropFriction")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",PressureDropHydrostatic")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",PressureDropTotal")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",LiquidHoldup")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HTCoverall")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HTCexternal")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HTCinternal")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HTCinsulation")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",HTCpipewall")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ThermalConductivityLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ThermalConductivityVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ReynoldsNumberLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ReynoldsNumberVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ViscosityLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",ViscosityVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",VolumetricFlowLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",VolumetricFlowVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",DensityLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",DensityVapor")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",SurfaceTension")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",InitialTemperature")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",FlowRegime")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",VelocityLiquid")
                    proplist.Add("HydraulicSegment," + ps.Key.ToString + ",Results," + j.ToString + ",VelocityVapor")
                    j += 1
                Next
            Next
            proplist.Add("ThermalProfile,CalculationType")
            proplist.Add("ThermalProfile,OverallHTC")
            proplist.Add("ThermalProfile,ExternalTemperatureDefinedHTC")
            proplist.Add("ThermalProfile,ExternalTemperatureGradientDefinedHTC")
            proplist.Add("ThermalProfile,ExternalTemperatureEstimatedHTC")
            proplist.Add("ThermalProfile,ExternalTemperatureGradientEstimatedHTC")
            proplist.Add("ThermalProfile,HeatExchanged")
            proplist.Add("ThermalProfile,IncludeWallHTC")
            proplist.Add("ThermalProfile,IncludeInternalHTC")
            proplist.Add("ThermalProfile,IncludeInsulationHTC")
            proplist.Add("ThermalProfile,InsulationThickness")
            proplist.Add("ThermalProfile,InsulationThermalConductivity")
            proplist.Add("ThermalProfile,IncludeExternalHTC")
            proplist.Add("ThermalProfile,ExternalEnvironmentType")
            proplist.Add("ThermalProfile,ExternalEnvironmentVelocityOrDeepness")
            Return proplist.ToArray(GetType(System.String))
            proplist = Nothing
        End Function

        Public Overrides Function SetPropertyValue(ByVal prop As String, ByVal propval As Object, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As Boolean

            If MyBase.SetPropertyValue(prop, propval, su) Then Return True

            If su Is Nothing Then su = New SystemsOfUnits.SI

            If prop.Contains("_") Then

                Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

                Select Case propidx
                    Case 2
                        Me.ThermalProfile.Calor_trocado = SystemsOfUnits.Converter.ConvertToSI(su.heatflow, propval)
                    Case 3
                        Me.OutletPressure = SystemsOfUnits.Converter.ConvertToSI(su.pressure, propval)
                    Case 4
                        Me.OutletTemperature = SystemsOfUnits.Converter.ConvertToSI(su.temperature, propval)
                    Case 5
                        Me.ThermalProfile.CGTC_Definido = SystemsOfUnits.Converter.ConvertToSI(su.heat_transf_coeff, propval)
                    Case 6
                        Me.ThermalProfile.Temp_amb_definir = SystemsOfUnits.Converter.ConvertToSI(su.temperature, propval)
                    Case 7
                        Me.ThermalProfile.AmbientTemperatureGradient = SystemsOfUnits.Converter.ConvertToSI(su.deltaT, propval) / SystemsOfUnits.Converter.ConvertToSI(su.distance, 1.0#)
                        Me.ThermalProfile.AmbientTemperatureGradient_EstimateHTC = SystemsOfUnits.Converter.ConvertToSI(su.deltaT, propval) / SystemsOfUnits.Converter.ConvertToSI(su.distance, 1.0#)
                End Select
            Else
                Try
                    If prop.Contains("HydraulicSegment") Then
                        Dim skey As Integer = prop.Split(",")(1)
                        Dim sprop As String = prop.Split(",")(2)
                        Select Case sprop
                            Case "Length"
                                Profile.Sections(skey).Comprimento = cv.ConvertToSI(su.distance, propval)
                            Case "Elevation"
                                Profile.Sections(skey).Elevacao = cv.ConvertToSI(su.distance, propval)
                            Case "InternalDiameter"
                                Profile.Sections(skey).DI = cv.Convert(su.diameter, "in.", propval)
                            Case "ExternalDiameter"
                                Profile.Sections(skey).DE = cv.Convert(su.diameter, "in.", propval)
                            Case "Sections"
                                Profile.Sections(skey).Incrementos = propval
                        End Select
                    ElseIf prop.Contains("ThermalProfile") Then
                        Dim tprop As String = prop.Split(",")(1)
                        Select Case tprop
                            Case "CalculationType"
                                ThermalProfile.TipoPerfil = propval
                            Case "OverallHTC"
                                ThermalProfile.CGTC_Definido = cv.ConvertToSI(su.heat_transf_coeff, propval)
                            Case "ExternalTemperatureDefinedHTC"
                                ThermalProfile.Temp_amb_definir = cv.ConvertToSI(su.temperature, propval)
                            Case "ExternalTemperatureEstimatedHTC"
                                ThermalProfile.Temp_amb_estimar = cv.ConvertToSI(su.temperature, propval)
                            Case "ExternalTemperatureGradientDefinedHTC"
                                ThermalProfile.AmbientTemperatureGradient = cv.ConvertToSI(su.deltaT, propval) / cv.ConvertToSI(su.distance, 1.0#)
                            Case "ExternalTemperatureGradientEstimatedHTC"
                                ThermalProfile.AmbientTemperatureGradient_EstimateHTC = cv.ConvertToSI(su.deltaT, propval) / cv.ConvertToSI(su.distance, 1.0#)
                            Case "HeatExchanged"
                                ThermalProfile.Calor_trocado = cv.ConvertToSI(su.heatflow, propval)
                            Case "IncludeWallHTC"
                                ThermalProfile.Incluir_paredes = propval
                            Case "IncludeInternalHTC"
                                ThermalProfile.Incluir_cti = propval
                            Case "IncludeInsulationHTC"
                                ThermalProfile.Incluir_isolamento = propval
                            Case "InsulationThickness"
                                ThermalProfile.Espessura = cv.ConvertToSI(su.thickness, propval)
                            Case "InsulationThermalConductivity"
                                ThermalProfile.Condtermica = cv.ConvertToSI(su.thermalConductivity, propval)
                            Case "IncludeExternalHTC"
                                ThermalProfile.Incluir_cte = propval
                            Case "ExternalEnvironmentType"
                                ThermalProfile.Meio = propval
                            Case "ExternalEnvironmentVelocityOrDeepness"
                                ThermalProfile.Velocidade = cv.ConvertToSI(su.velocity, propval)
                        End Select
                    End If
                Catch ex As Exception
                    FlowSheet.ShowMessage("Error setting property '" + prop + "': " + ex.Message, IFlowsheet.MessageType.GeneralError)
                End Try
            End If

            Return 1

        End Function

        Public Overrides Function GetPropertyUnit(ByVal prop As String, Optional ByVal su As Interfaces.IUnitsOfMeasure = Nothing) As String
            Dim u0 As String = MyBase.GetPropertyUnit(prop, su)

            If u0 <> "NF" Then
                Return u0
            ElseIf prop.Contains("_") Then
                If su Is Nothing Then su = New SystemsOfUnits.SI
                Dim value As String = ""
                Dim propidx As Integer = Convert.ToInt32(prop.Split("_")(2))

                Select Case propidx
                    Case 0
                        value = su.deltaP
                    Case 1
                        value = su.deltaT
                    Case 2
                        value = su.heatflow
                    Case 3
                        value = su.pressure
                    Case 4
                        value = su.temperature
                    Case 5
                        value = su.heat_transf_coeff
                    Case 6
                        value = su.temperature
                    Case 7
                        value = su.deltaT & "/" & su.distance
                End Select
                Return value
            Else
                If prop.Contains("Results") Then
                    Dim sprop As String = prop.Split(",")(4)
                    Select Case sprop
                        Case "HeatTransfer"
                            Return su.heatflow
                        Case "HeatCapacityLiquid"
                            Return su.heatCapacityCp
                        Case "HeatCapacityVapor"
                            Return su.heatCapacityCp
                        Case "PressureDropFriction"
                            Return su.deltaP
                        Case "PressureDropHydrostatic"
                            Return su.deltaP
                        Case "PressureDropTotal"
                            Return su.deltaP
                        Case "LiquidHoldup"
                            Return ""
                        Case "HTCoverall"
                            Return su.heat_transf_coeff
                        Case "HTCexternal"
                            Return su.heat_transf_coeff
                        Case "HTCinternal"
                            Return su.heat_transf_coeff
                        Case "HTCinsulation"
                            Return su.heat_transf_coeff
                        Case "HTCpipewall"
                            Return su.heat_transf_coeff
                        Case "ThermalConductivityLiquid"
                            Return su.thermalConductivity
                        Case "ThermalConductivityVapor"
                            Return su.thermalConductivity
                        Case "ReynoldsNumberLiquid"
                            Return ""
                        Case "ReynoldsNumberVapor"
                            Return ""
                        Case "ViscosityLiquid"
                            Return su.viscosity
                        Case "ViscosityVapor"
                            Return su.viscosity
                        Case "VolumetricFlowLiquid"
                            Return su.volumetricFlow
                        Case "VolumetricFlowVapor"
                            Return su.volumetricFlow
                        Case "DensityLiquid"
                            Return su.density
                        Case "DensityVapor"
                            Return su.density
                        Case "SurfaceTension"
                            Return su.surfaceTension
                        Case "InitialTemperature"
                            Return su.temperature
                        Case "FlowRegime"
                            Return ""
                        Case "VelocityLiquid"
                            Return su.velocity
                        Case "VelocityVapor"
                            Return su.velocity
                    End Select
                ElseIf prop.Contains("HydraulicSegment") Then
                    Dim skey As Integer = prop.Split(",")(1)
                    Dim sprop As String = prop.Split(",")(2)
                    Select Case sprop
                        Case "Length"
                            Return su.distance
                        Case "Elevation"
                            Return su.distance
                        Case "InternalDiameter"
                            Return su.diameter
                        Case "ExternalDiameter"
                            Return su.diameter
                        Case "Sections"
                            Return ""
                        Case Else
                            Return 0.0
                    End Select
                ElseIf prop.Contains("ThermalProfile") Then
                    Dim tprop As String = prop.Split(",")(1)
                    Select Case tprop
                        Case "CalculationType"
                            Return ""
                        Case "OverallHTC"
                            Return su.heat_transf_coeff
                        Case "ExternalTemperatureDefinedHTC"
                            Return su.temperature
                        Case "ExternalTemperatureEstimatedHTC"
                            Return su.temperature
                        Case "ExternalTemperatureGradientDefinedHTC"
                            Return su.deltaT & "/" & su.distance
                        Case "ExternalTemperatureGradientEstimatedHTC"
                            Return su.deltaT & "/" & su.distance
                        Case "HeatExchanged"
                            Return su.heatflow
                        Case "IncludeWallHTC"
                            Return ""
                        Case "IncludeInternalHTC"
                            Return ""
                        Case "IncludeInsulationHTC"
                            Return ""
                        Case "InsulationThickness"
                            Return su.thickness
                        Case "InsulationThermalConductivity"
                            Return su.thermalConductivity
                        Case "IncludeExternalHTC"
                            Return ""
                        Case "ExternalEnvironmentType"
                            Return ""
                        Case "ExternalEnvironmentVelocityOrDeepness"
                            Return su.velocity
                        Case Else
                            Return ""
                    End Select
                Else
                    Return ""
                End If
            End If
        End Function

        Public Overrides Sub DisplayEditForm()

            If f Is Nothing Then
                f = New EditingForm_Pipe With {.SimObject = Me}
                f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                Me.FlowSheet.DisplayForm(f)
            Else
                If f.IsDisposed Then
                    f = New EditingForm_Pipe With {.SimObject = Me}
                    f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                    Me.FlowSheet.DisplayForm(f)
                Else
                    f.Activate()
                End If
            End If

        End Sub

        Public Overrides Sub UpdateEditForm()
            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    f.UIThread(Sub() f.UpdateInfo())
                End If
            End If
        End Sub

        Public Overrides Function GetIconBitmap() As Object
            Return My.Resources.uo_pipe_32
        End Function

        Public Overrides Function GetDisplayDescription() As String
            Return ResMan.GetLocalString("PIPE_Desc")
        End Function

        Public Overrides Function GetDisplayName() As String
            Return ResMan.GetLocalString("PIPE_Name")
        End Function

        Public Overrides Sub CloseEditForm()
            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    f.Close()
                    f = Nothing
                End If
            End If
        End Sub

        Public Overrides ReadOnly Property MobileCompatible As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overrides Function GetReport(su As IUnitsOfMeasure, ci As Globalization.CultureInfo, numberformat As String) As String

            Dim str As New Text.StringBuilder

            Dim istr, ostr As MaterialStream
            istr = Me.GetInletMaterialStream(0)
            ostr = Me.GetOutletMaterialStream(0)

            istr.PropertyPackage.CurrentMaterialStream = istr

            str.AppendLine("Pipe Segment: " & Me.GraphicObject.Tag)
            str.AppendLine("Property Package: " & Me.PropertyPackage.ComponentName)
            str.AppendLine()
            str.AppendLine("Inlet conditions")
            str.AppendLine()
            str.AppendLine("    Temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, istr.Phases(0).Properties.temperature.GetValueOrDefault).ToString(numberformat, ci) & " " & su.temperature)
            str.AppendLine("    Pressure: " & SystemsOfUnits.Converter.ConvertFromSI(su.pressure, istr.Phases(0).Properties.pressure.GetValueOrDefault).ToString(numberformat, ci) & " " & su.pressure)
            str.AppendLine("    Mass flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.massflow, istr.Phases(0).Properties.massflow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.massflow)
            str.AppendLine("    Volumetric flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.volumetricFlow, istr.Phases(0).Properties.volumetric_flow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.volumetricFlow)
            str.AppendLine("    Vapor fraction: " & istr.Phases(2).Properties.molarfraction.GetValueOrDefault.ToString(numberformat, ci))
            str.AppendLine("    Compounds: " & istr.PropertyPackage.RET_VNAMES.ToArrayString)
            str.AppendLine("    Molar composition: " & istr.PropertyPackage.RET_VMOL(PropertyPackages.Phase.Mixture).ToArrayString(ci))
            str.AppendLine()
            str.AppendLine("Results")
            str.AppendLine()
            str.AppendLine("    Pressure Change: " & SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, Me.DeltaP.GetValueOrDefault).ToString(numberformat, ci) & " " & su.deltaP)
            str.AppendLine("    Temperature Change: " & SystemsOfUnits.Converter.ConvertFromSI(su.deltaT, Me.DeltaT.GetValueOrDefault).ToString(numberformat, ci) & " " & su.deltaT)
            str.AppendLine("    Heat balance: " & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, Me.DeltaQ.GetValueOrDefault).ToString(numberformat, ci) & " " & su.heatflow)
            str.AppendLine()
            str.AppendLine("Outlet conditions")
            str.AppendLine()
            ostr.PropertyPackage.CurrentMaterialStream = ostr
            str.AppendLine("    Temperature: " & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, ostr.Phases(0).Properties.temperature.GetValueOrDefault).ToString(numberformat, ci) & " " & su.temperature)
            str.AppendLine("    Pressure: " & SystemsOfUnits.Converter.ConvertFromSI(su.pressure, ostr.Phases(0).Properties.pressure.GetValueOrDefault).ToString(numberformat, ci) & " " & su.pressure)
            str.AppendLine("    Mass flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.massflow, ostr.Phases(0).Properties.massflow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.massflow)
            str.AppendLine("    Volumetric flow: " & SystemsOfUnits.Converter.ConvertFromSI(su.volumetricFlow, ostr.Phases(0).Properties.volumetric_flow.GetValueOrDefault).ToString(numberformat, ci) & " " & su.volumetricFlow)
            str.AppendLine("    Vapor fraction: " & ostr.Phases(2).Properties.molarfraction.GetValueOrDefault.ToString(numberformat, ci))

            Dim comp_ant As Double = 0

            str.AppendLine()
            str.AppendLine("Elevation Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Elevation (" & su.distance & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.distance, (Math.Atan(ps.Elevacao / (ps.Comprimento ^ 2 - ps.Elevacao ^ 2) ^ 0.5) * 180 / Math.PI)).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Pressure Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Pressure (" & su.pressure & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.pressure, res.PressaoInicial.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next


            str.AppendLine()
            str.AppendLine("Friction Pressure Drop Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Pressure Drop (" & su.deltaP & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, res.DpPorFriccao).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Hydrostatic Pressure Drop Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Pressure Drop (" & su.deltaP & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.deltaP, res.DpPorHidrostatico).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Temperature Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Temperature (" & su.temperature & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.temperature, res.TemperaturaInicial.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Liquid Velocity Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Liquid Velocity (" & su.velocity & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.velocity, res.LiqVel.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Vapor Velocity Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Vapor Velocity (" & su.velocity & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.velocity, res.VapVel.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Liquid Reynolds Number Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Liquid Re")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & res.LiqRe.GetValueOrDefault.ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Vapor Reynolds Number Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Vapor Re")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & res.VapRe.GetValueOrDefault.ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Liquid Holdup Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Liquid Holdup")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & res.HoldupDeLiquido.GetValueOrDefault.ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Flow Pattern Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Flow Pattern")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & res.TipoFluxo)
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Heat Exchange Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Heat Exchanged (" & su.heatflow & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, res.CalorTransferido.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Overall HTC Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Overall HTC (" & su.heat_transf_coeff & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC.GetValueOrDefault).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Internal HTC Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Internal HTC (" & su.heat_transf_coeff & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_internal).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Pipe Wall HTC Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Pipe Wall HTC (" & su.heat_transf_coeff & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_pipewall).ToString(numberformat, ci))
                Next
                If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                    comp_ant += ps.Comprimento / ps.Incrementos
                End If
            Next

            str.AppendLine()
            str.AppendLine("Insulation HTC Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Insulation HTC (" & su.heat_transf_coeff & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_insulation).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("External HTC Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "External HTC (" & su.heat_transf_coeff & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_external).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            str.AppendLine()
            str.AppendLine("Energy Flow Profile")
            str.AppendLine()
            str.AppendLine("Length (" & su.distance & ")" & vbTab & "Energy Flow (" & su.heatflow & ")")
            comp_ant = 0
            For Each ps In Profile.Sections.Values
                For Each res In ps.Resultados
                    str.AppendLine(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant).ToString(numberformat, ci) &
                                   vbTab & SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, res.EnergyFlow_Inicial).ToString(numberformat, ci))
                    If ps.TipoSegmento = "Tubulaosimples" Or ps.TipoSegmento = "" Or ps.TipoSegmento = "Straight Tube Section" Then
                        comp_ant += ps.Comprimento / ps.Incrementos
                    End If
                Next
            Next

            Return str.ToString

        End Function

        Public Overrides Function GetPropertyDescription(p As String) As String
            If p.Equals("Calculation Mode") Then
                Return "Select the calculation mode of this pipe segment model. 'Specify Length' is the default one and will calculate outlet pressure and temperature for a defined hydraulic profile. 'Specify Outlet Pressure/Temperature' will calculate the length for a single straight tube segment that results in the specified variable  (outlet P or T) value."
            ElseIf p.Equals("Outlet Pressure") Then
                Return "If the calculation mode is 'Outlet Pressure', enter the desired value."
            ElseIf p.Equals("Outlet Temperature") Then
                Return "If the calculation mode is 'Outlet Temperature', enter the desired value."
            ElseIf p.Equals("Pressure Convergence Tolerance") Then
                Return "Define the tolerance for the pressure loop convergence of a segment."
            ElseIf p.Equals("Temperature Convergence Tolerance") Then
                Return "Define the tolerance for the temperature loop convergence of a segment."
            ElseIf p.Equals("Include Joule-Thomson Effect") Then
                Return "Includes the Joule-Thomson effect in the calculation of the fluid temperature as it flows through the pipe."
            Else
                Return p
            End If
        End Function

        Public Overrides Function GetChartModelNames() As List(Of String)
            Return New List(Of String)({"Temperature Profile", "Pressure Profile", "Heat Flow Profile", "Liquid Velocity Profile", "Vapor Velocity Profile", "Liquid Holdup Profile", "Inclination Profile", "Overall HTC Profile", "Internal HTC Profile", "Wall k/L Profile", "Insulation k/L Profile", "External HTC Profile"})
        End Function

        Public Overrides Function GetChartModel(name As String) As Object

            Dim su = FlowSheet.FlowsheetOptions.SelectedUnitSystem

            Dim model = New PlotModel() With {.Subtitle = name, .Title = GraphicObject.Tag}

            model.TitleFontSize = 11
            model.SubtitleFontSize = 10

            model.Axes.Add(New LinearAxis() With { _
                .MajorGridlineStyle = LineStyle.Dash, _
                .MinorGridlineStyle = LineStyle.Dot, _
                .Position = AxisPosition.Bottom, _
                .FontSize = 10, _
                .Title = "Length (" + su.distance + ")" _
            })

            model.Axes.Add(New LinearAxis() With { _
                .MajorGridlineStyle = LineStyle.Dash, _
                .MinorGridlineStyle = LineStyle.Dot, _
                .Position = AxisPosition.Left, _
                .FontSize = 10
            })

            model.LegendFontSize = 11
            model.LegendPlacement = LegendPlacement.Outside
            model.LegendOrientation = LegendOrientation.Horizontal
            model.LegendPosition = LegendPosition.BottomCenter
            model.TitleHorizontalAlignment = TitleHorizontalAlignment.CenteredWithinView

            Dim px = PopulateData(0)

            Select Case name
                Case "Temperature Profile"
                    model.AddLineSeries(px, PopulateData(3))
                    model.Axes(1).Title = "Temperature (" + su.temperature + ")"
                Case "Pressure Profile"
                    model.AddLineSeries(px, PopulateData(2))
                    model.Axes(1).Title = "Pressure (" + su.pressure + ")"
                Case "Heat Flow Profile"
                    model.AddLineSeries(px, PopulateData(6))
                    model.Axes(1).Title = "Heat Flow (" + su.heatflow + ")"
                Case "Liquid Velocity Profile"
                    model.AddLineSeries(px, PopulateData(4))
                    model.Axes(1).Title = "Velocity (" + su.velocity + ")"
                Case "Vapor Velocity Profile"
                    model.AddLineSeries(px, PopulateData(5))
                    model.Axes(1).Title = "Velocity (" + su.velocity + ")"
                Case "Inclination Profile"
                    model.AddLineSeries(px, PopulateData(1))
                    model.Axes(1).Title = "Elevation (" + su.distance + ")"
                Case "Liquid Holdup Profile"
                    model.AddLineSeries(px, PopulateData(7))
                    model.Axes(1).Title = "Holdup"
                Case "Overall HTC Profile"
                    model.AddLineSeries(px, PopulateData(8))
                    model.Axes(1).Title = "Heat Transfer Coefficient (" + su.heat_transf_coeff + ")"
                Case "Internal HTC Profile"
                    model.AddLineSeries(px, PopulateData(9))
                    model.Axes(1).Title = "Heat Transfer Coefficient (" + su.heat_transf_coeff + ")"
                Case "Wall k/L Profile"
                    model.AddLineSeries(px, PopulateData(10))
                    model.Axes(1).Title = "Heat Transfer Coefficient (" + su.heat_transf_coeff + ")"
                Case "Insulation k/L Profile"
                    model.AddLineSeries(px, PopulateData(11))
                    model.Axes(1).Title = "Heat Transfer Coefficient (" + su.heat_transf_coeff + ")"
                Case "External HTC Profile"
                    model.AddLineSeries(px, PopulateData(12))
                    model.Axes(1).Title = "Heat Transfer Coefficient (" + su.heat_transf_coeff + ")"
            End Select

            Return model

        End Function

        Private Function PopulateData(position As Integer) As List(Of Double)
            Dim su = FlowSheet.FlowsheetOptions.SelectedUnitSystem
            Dim vec As New List(Of Double)()
            Select Case position
                Case 0
                    'distance
                    Dim comp_ant As Double = 0.0F
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.distance, comp_ant))
                            comp_ant += sec.Comprimento / sec.Incrementos
                        Next
                    Next
                    Exit Select
                Case 1
                    'elevation
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(Math.Atan(sec.Elevacao / Math.Pow(Math.Pow(sec.Comprimento, 2) - Math.Pow(sec.Elevacao, 2), 0.5) * 180 / Math.PI))
                        Next
                    Next
                    Exit Select
                Case 2
                    'pressure
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.pressure, res.PressaoInicial.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 3
                    'temperaturee
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.temperature, res.TemperaturaInicial.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 4
                    'vel liqe
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.velocity, res.LiqVel.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 5
                    'vel vape
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.velocity, res.VapVel.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 6
                    'heatflowe
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heatflow, res.CalorTransferido.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 7
                    'liqholde
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(res.HoldupDeLiquido.GetValueOrDefault())
                        Next
                    Next
                    Exit Select
                Case 8
                    'OHTCe
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC.GetValueOrDefault()))
                        Next
                    Next
                    Exit Select
                Case 9
                    'IHTCC
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_internal))
                        Next
                    Next
                    Exit Select
                Case 10
                    'IHTC
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_pipewall))
                        Next
                    Next
                    Exit Select
                Case 11
                    'IHTC
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_insulation))
                        Next
                    Next
                    Exit Select
                Case 12
                    'EHTC
                    For Each sec In Profile.Sections.Values
                        For Each res In sec.Resultados
                            vec.Add(SystemsOfUnits.Converter.ConvertFromSI(su.heat_transf_coeff, res.HTC_external))
                        Next
                    Next
                    Exit Select
            End Select
            Return vec
        End Function

    End Class

End Namespace