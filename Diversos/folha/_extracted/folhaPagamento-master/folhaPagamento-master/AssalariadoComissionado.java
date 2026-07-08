package folhaPagamento;

public class AssalariadoComissionado extends Funcionario {
	double salBase;
	double percentual;
	double totVendas;
	
	public AssalariadoComissionado (String nome, String setor, String dataAdm, double salBase, double percentual, double totVendas) {
		super (nome, setor, dataAdm);
		this.setSalBase(salBase);
		this.setPercentual(percentual);
		this.setTotVendas(totVendas);
	}
	
	public double getCalculoAssalComiss (double salFixo, double totVendas, double percentual) {
		double resultado;
		
		resultado = (totVendas * percentual) + salFixo;
		
		return resultado;
	}
	
	public double getCalculoNovoAssalComiss (double salFixo, double totVendas, double percentual, double percentualAumento) {
		double resultado;
		
		resultado = getSalBaseNovo(0.10) + (getTotVendas() * getPercentual());
		
		return resultado;
	}
	
	public double getPercAumento(double percentualAumento) {
		return percentualAumento;
	}

	public double getSalBase() {
		return salBase;
	}
	
	public double getSalBaseNovo(double percAumento) {
		double resultado;
		
		resultado = (getSalBase() * percAumento) + getSalBase();
		
		return resultado;
	}

	public void setSalBase(double salBase) {
		this.salBase = salBase;
	}

	public double getPercentual() {
		return percentual;
	}

	public void setPercentual(double percentual) {
		this.percentual = percentual;
	}

	public double getTotVendas() {
		return totVendas;
	}

	public void setTotVendas(double totVendas) {
		this.totVendas = totVendas;
	}
	
	
}
